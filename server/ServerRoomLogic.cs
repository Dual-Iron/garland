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

    ServerRoom[]? rooms = default;

    // TODO: re-abstractizing rooms lol

    public ServerRoomLogic(RainWorldGame game, ServerSession session)
    {
        this.game = game;
        this.session = session;
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
                if (peer.RoomStates[roomIndex] == RoomState.Synced) {
                    Update(peer, room.realizedRoom);
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
            if (player == null || trackedPeers.Any(p => p.Player.ID == player.ID)) {
                continue;
            }

            trackedPeers.Add(new(player, peer, new RoomState[game.world.abstractRooms.Length]));
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
        Main.Log.LogDebug($"Introduce {room.abstractRoom.name} to player {peer.Player.ID.number}");
    }

    private void Update(TrackedPeer peer, Room room)
    {
        Main.Log.LogDebug($"Updating {room.abstractRoom.name} for player {peer.Player.ID.number}");
    }
}
