using Common;
using System.Linq;
using static Creature.Grasp.Shareability;

namespace Client;

sealed class ClientRoomLogic
{
    readonly RainWorldGame game;
    readonly ClientSession session;

    public ClientRoomLogic(RainWorldGame game, ClientSession session)
    {
        this.game = game;
        this.session = session;
    }

    public bool TryFind<T>(int id, out T obj) where T : PhysicalObject
    {
        if (session.Objects.TryGetValue(id, out var something) && something is T t) {
            obj = t;
            return true;
        }
        obj = null!;
        return false;
    }

    public void UpdatePreRoom()
    {
        // Follow our own player
        game.cameras[0].followAbstractCreature = session.MyPlayer;

        var room = session.MyPlayer?.Room.realizedRoom;
        if (room != null && game.cameras[0].room != room) {
            game.cameras[0].MoveCamera(room, 0);
        }

        foreach (var packet in RealizeRoom.All()) {
            game.world.ActivateRoom(packet.Room);
        }

        foreach (var packet in AbstractizeRoom.All()) {
            game.world.GetAbstractRoom(packet.Room).Abstractize();
        }

        foreach (var packet in DestroyObject.All()) {
            if (TryFind(packet.ID, out PhysicalObject obj) && !obj.slatedForDeletetion) {
                Main.Log.LogDebug($"Server destroyed {obj.DebugName()}");

                obj.abstractPhysicalObject.Destroy();
                obj.Destroy();
                session.Objects.Remove(packet.ID);
            }
        }

        foreach (var packet in KillCreature.All()) {
            if (session.Objects.TryGetValue(packet.ID, out var obj) && obj is Creature crit) {
                Main.Log.LogDebug($"Server killed {obj.DebugName()}");

                crit.Die();
            }
        }

        IntroduceObjects();

        UpdateObjects();
    }

    public void UpdatePostRoom()
    {
        try {
            Main.GrabPacket = true;
            DoUpdatePostRoom();
        }
        finally {
            Main.GrabPacket = false;
        }
    }

    private void DoUpdatePostRoom()
    {
        // Update some things after object updates, to give the client a chance to run sfx/vfx and stuff.
        // These are mostly "corrective" packets that ensure the client's world is up-to-date with the server.
        foreach (var packet in Grab.All()) {
            if (TryFind(packet.GrabberID, out Creature grabber) && grabber.grasps != null && packet.GraspUsed < grabber.grasps.Length && TryFind(packet.GrabbedID, out PhysicalObject grabbed)) {
                grabber.ReleaseGrasp(packet.GraspUsed);

                var share = packet.NonExclusive
                    ? NonExclusive
                    : packet.ShareWithNonExclusive
                        ? CanOnlyShareWithNonExclusive
                        : CanNotShare;

                grabber.Grab(grabbed, packet.GraspUsed, packet.GrabbedChunk, share, packet.Dominance, packet.OverrideEquallyDominant, packet.Pacifying);
            }
        }

        foreach (var packet in Release.All()) {
            if (TryFind(packet.GrabberID, out Creature grabber) && grabber.grasps != null && packet.GraspUsed < grabber.grasps.Length && TryFind(packet.GrabbedID, out PhysicalObject grabbed)) {
                // If grabbing the object, but in a desynced grasp, then switch grasps.
                var serverGrasp = grabber.grasps[packet.GraspUsed];
                var clientGrasp = grabber.grasps.FirstOrDefault(g => g?.grabbed == grabbed);
                if (serverGrasp?.grabbed != grabbed && clientGrasp != null) {
                    grabber.SwitchGrasps(packet.GraspUsed, clientGrasp.graspUsed);
                }
                grabber.ReleaseGrasp(packet.GraspUsed);
            }
        }
    }

    private void IntroduceObjects()
    {
        foreach (var packet in IntroPlayer.All()) {
            // Set this before realizing player (so slugcatStats is not null)
            session.ClientData[packet.ID] = SharedPlayerData.FromPacket(packet);

            AbstractCreature p = new(game.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, new(game.world.GetAbstractRoom(packet.Room).index, 0, 0, -1), new(-1, packet.ID));
            p.state = new PlayerState(p, playerNumber: packet.ID, slugcatCharacter: new("Garland Player " + packet.ID), false);
            p.Room.AddEntity(p);
            p.RealizeInRoom();

            ((Player)p.realizedObject).glowing = packet.Glows;

            session.Objects[packet.ID] = p.realizedObject;
            session.AddPlayer(p);

            Main.Log.LogDebug($"Introduced player {packet.ID}");
        }
    }

    private void UpdateObjects()
    {
        session.UpdatePlayer.Clear();
        foreach (var packet in UpdatePlayer.All()) {
            if (TryFind(packet.ID, out Player _)) {
                session.PlayerLastInput[packet.ID] = new(packet.InputDir0, packet.InputBitmask0);
                session.UpdatePlayer[packet.ID] = packet;
            }
        }
    }
}
