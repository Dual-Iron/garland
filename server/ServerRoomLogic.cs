using Common;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server;

sealed class ServerRoomLogic
{
    enum RoomState : byte { Abstract, Unsynced, Synced }

    record struct ServerRoom(int? LastVisit);

    record struct TrackedPeer(AbstractCreature Player, NetPeer NetPeer, RoomState[] RoomStates)
    {
        public readonly List<int> RealizedRooms = new();
        public readonly List<int> RealizedObjects = new();
    }

    static readonly RoomRealizer fake = new(null, null);
    static float PerformanceEstimate(AbstractRoom room, float budget = 1500f)
    {
        fake.performanceBudget = budget;
        return fake.RoomPerformanceEstimation(room);
    }

    readonly RainWorldGame game;
    readonly ServerSession session;
    readonly List<TrackedPeer> trackedPeers = new();

    ServerRoom[] rooms = default!;

    // TODO: re-abstractizing rooms lol

    public void ObjectUpdate<T>(PhysicalObject p, T packet) where T : IPacket
    {
        int roomIndex = p.abstractPhysicalObject.pos.room;

        foreach (TrackedPeer peer in trackedPeers) {
            if (peer.RoomStates[roomIndex] == RoomState.Synced) {
                peer.NetPeer.Send(packet, DeliveryMethod.ReliableSequenced);
            }
        }
    }

    public ServerRoomLogic(RainWorldGame game, ServerSession session)
    {
        this.game = game;
        this.session = session;

        On.AbstractPhysicalObject.Realize += SyncRealize;
        On.AbstractPhysicalObject.Abstractize += SyncAbstractize;
    }

    private void SyncRealize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
    {
        bool actuallyRealizing = self.realizedObject == null;
        orig(self);
        if (actuallyRealizing && self.realizedObject != null) {
            foreach (TrackedPeer peer in trackedPeers) {
                LoadObject(peer, self.realizedObject);
            }
        }
    }

    private void SyncAbstractize(On.AbstractPhysicalObject.orig_Abstractize orig, AbstractPhysicalObject self, WorldCoordinate coord)
    {
        if (self.realizedObject != null) {
            foreach (TrackedPeer peer in trackedPeers) {
                UnloadObject(peer, self.ID.number);
            }
        }
        orig(self, coord);
    }

    // Load an object if it's relevant to the client
    private void LoadObject(TrackedPeer peer, PhysicalObject o)
    {
        if (peer.RoomStates[o.abstractPhysicalObject.Room.index] == RoomState.Synced) {
            Introduce(peer, o);
        }
    }

    // Unload an object if it's relevant to the client
    private void UnloadObject(TrackedPeer peer, int id)
    {
        int i = peer.RealizedObjects.IndexOf(id);
        if (i >= 0) {
            peer.RealizedObjects.RemoveAt(i);
            peer.NetPeer.Send(new DestroyObject(id));
        }
    }

    // Unload an entire room for a peer
    private void UnloadForPeer(TrackedPeer peer, AbstractRoom room)
    {
        peer.RoomStates[room.index] = RoomState.Abstract;
        if (peer.RealizedRooms.Remove(room.index)) {
            peer.NetPeer.Send(new AbstractizeRoom(room.index));
        }

        foreach (var entity in room.entities) {
            if (entity is AbstractPhysicalObject apo && apo.realizedObject != null) {
                UnloadObject(peer, apo.ID.number);
            }
        }
    }

    // Unload an entire room every peer, and the server
    private void UnloadForServer(AbstractRoom room)
    {
        rooms[room.index].LastVisit = null;

        foreach (var entity in room.entities) {
            if (entity is AbstractPhysicalObject apo && apo.realizedObject != null) {
                foreach (var peer in trackedPeers) {
                    UnloadObject(peer, apo.ID.number);
                }
            }
        }

        foreach (var peer in trackedPeers) {
            peer.RoomStates[room.index] = RoomState.Abstract;
            if (peer.RealizedRooms.Remove(room.index)) {
                peer.NetPeer.Send(new AbstractizeRoom(room.index));
            }
        }

        room.Abstractize();
    }

    public void Update()
    {
        rooms ??= new ServerRoom[game.world.abstractRooms.Length];

        UpdatePeerList();

        foreach (TrackedPeer peer in trackedPeers) {
            UpdatePeer(peer);
        }

        foreach (TrackedPeer peer in trackedPeers) {
            foreach (int roomIndex in peer.RealizedRooms) {
                AbstractRoom room = game.world.GetAbstractRoom(roomIndex);
                if (peer.RoomStates[roomIndex] == RoomState.Unsynced && room.realizedRoom.fullyLoaded) {
                    peer.RoomStates[roomIndex] = RoomState.Synced;

                    Introduce(peer, room.realizedRoom);
                }
                if (peer.Player.Room.index == roomIndex) {
                    rooms[roomIndex].LastVisit = game.clock;
                }
            }
        }
    }

    private void UpdatePeerList()
    {
        // Look for new untracked peers
        foreach (var peer in Main.Instance.server.ConnectedPeerList) {
            var player = session.GetPlayer(peer);
            if (player != null && !trackedPeers.Any(p => p.Player.ID == player.ID)) {
                trackedPeers.Add(new(player, peer, new RoomState[game.world.abstractRooms.Length]));
            }
        }
        // Remove old peers that have disconnected
        for (int i = trackedPeers.Count - 1; i >= 0; i--) {
            if (trackedPeers[i].NetPeer.ConnectionState == ConnectionState.Disconnected) {
                trackedPeers.RemoveAt(i);
            }
        }
    }

    private void UpdatePeer(TrackedPeer peer)
    {
        Realize(peer, peer.Player.Room);

        foreach (var connection in peer.Player.Room.connections) {
            if (connection > -1) {
                Realize(peer, game.world.GetAbstractRoom(connection));
            }
        }
    }

    private void Realize(TrackedPeer peer, AbstractRoom room)
    {
        if (peer.RoomStates[room.index] == RoomState.Abstract) {
            Main.Log.LogDebug($"Requesting player {peer.Player.ID.number} to realize {room.name}");

            peer.RoomStates[room.index] = RoomState.Unsynced;
            peer.RealizedRooms.Add(room.index);

            // Tell client to realize the room if it hasn't already
            peer.NetPeer.Send(new RealizeRoom(room.index));

            // Start realizing the room on the server
            room.world.ActivateRoom(room);
        }
    }

    private void Introduce(TrackedPeer peer, Room room)
    {
        foreach (var entity in room.abstractRoom.entities) {
            if (entity is AbstractPhysicalObject apo && apo.realizedObject != null) {
                Introduce(peer, apo.realizedObject);
            }
        }
    }

    private void Introduce(TrackedPeer peer, PhysicalObject realizedObject)
    {
        peer.RealizedObjects.Add(realizedObject.abstractPhysicalObject.ID.number);

        int id = realizedObject.abstractPhysicalObject.ID.number;
        if (realizedObject is Player p) {
            SharedPlayerData data = p.Data() ?? new();
            IntroPlayer intro = new() {
                ID = id,
                Room = p.abstractPhysicalObject.Room.index,
                SkinR = data.SkinColor.r,
                SkinG = data.SkinColor.g,
                SkinB = data.SkinColor.b,
                RunSpeed = data.Stats.runspeedFac,
                PoleClimbSpeed = data.Stats.poleClimbSpeedFac,
                CorridorClimbSpeed = data.Stats.corridorClimbSpeedFac,
                BodyWeight = data.Stats.bodyWeightFac,
                Lungs = data.Stats.lungsFac,
                Loudness = data.Stats.loudnessFac,
                Stealth = data.Stats.visualStealthInSneakMode,
                VisBonus = data.Stats.generalVisibilityBonus,
                ThrowingSkill = (byte)data.Stats.throwingSkill,
                SleepFood = (byte)data.Stats.foodToHibernate,
                MaxFood = (byte)data.Stats.maxFood,
                Bitmask = IntroPlayer.ToBitmask(data.Stats.malnourished, Glows: false, HasMark: false),
            };
            peer.NetPeer.Send(intro);
        }
    }
}
