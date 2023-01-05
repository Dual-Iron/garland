using Common;
using Vec = UnityEngine.Vector2;

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

    public void Update()
    {
        // Follow our own player
        game.cameras[0].followAbstractCreature = session.MyPlayer;

        if (session.MyPlayer?.Room.realizedRoom != null && game.cameras[0].room != session.MyPlayer.Room.realizedRoom) {
            game.cameras[0].MoveCamera(session.MyPlayer.Room.realizedRoom, 0);
        }

        foreach (var packet in RealizeRoom.All()) {
            Main.Log.LogDebug($"Respecting request to realize {game.world.GetAbstractRoom(packet.Index).name}");
            game.world.ActivateRoom(packet.Index);
        }

        foreach (var packet in AbstractizeRoom.All()) {
            game.world.GetAbstractRoom(packet.Index).Abstractize();
        }

        foreach (var packet in DestroyObject.All()) {
            if (session.Objects.TryGetValue(packet.ID, out var obj) && !obj.slatedForDeletetion) {
                obj.abstractPhysicalObject.Destroy();
                obj.Destroy();
                session.Objects.Remove(packet.ID);
                Main.Log.LogDebug($"Server destroyed {obj.DebugName()}");
            }
        }

        IntroduceStuff();

        UpdateStuff();

        foreach (var packet in KillCreature.All()) {
            if (session.Objects.TryGetValue(packet.ID, out var obj) && obj is Creature crit) {
                Main.Log.LogDebug($"Server killed {obj.DebugName()}");
                crit.Die();
            }
        }
    }

    private void IntroduceStuff()
    {
        foreach (var packet in IntroPlayer.All()) {
            // Set this before realizing player (so slugcatStats is not null)
            session.ClientData[packet.ID] = new SharedPlayerData() {
                SkinColor = new UnityEngine.Color32(packet.SkinR, packet.SkinG, packet.SkinB, 255),
                HasMark = packet.HasMark, // TODO HasMark and other graphical/non-graphical changes
                Glows = packet.Glows,
                Stats = new SlugcatStats(0, false) {
                    runspeedFac = packet.RunSpeed,
                    poleClimbSpeedFac = packet.PoleClimbSpeed,
                    corridorClimbSpeedFac = packet.CorridorClimbSpeed,
                    bodyWeightFac = packet.BodyWeight,
                    lungsFac = packet.Lungs,
                    loudnessFac = packet.Loudness,
                    visualStealthInSneakMode = packet.Stealth,
                    generalVisibilityBonus = packet.VisBonus,
                    throwingSkill = packet.ThrowingSkill,
                    foodToHibernate = packet.SleepFood,
                    maxFood = packet.MaxFood,
                    malnourished = packet.Ill,
                }
            };

            AbstractCreature p = new(game.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, new(packet.Room, 0, 0, -1), new(-1, packet.ID));
            p.state = new PlayerState(p, packet.ID, 0, false);
            p.Room.AddEntity(p);
            p.RealizeInRoom();

            ((Player)p.realizedObject).glowing = packet.Glows;

            session.Objects[packet.ID] = p.realizedObject;
            session.AddPlayer(p);
        }
    }

    private void UpdateStuff()
    {
        foreach (var packet in UpdatePlayer.All()) {
            if (session.Objects.TryGetValue(packet.ID, out var obj) && obj is Player p) {
                session.LastInput[packet.ID] = new(packet.InputDir0, packet.InputBitmask0);
                session.UpdatePlayerCache[packet.ID] = packet;
            }
        }
    }
}
