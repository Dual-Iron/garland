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
            }
        }

        IntroduceStuff();

        UpdateStuff();

        foreach (var packet in KillCreature.All()) {
            if (session.Objects.TryGetValue(packet.ID, out var obj) && obj is Creature crit) {
                Main.Log.LogDebug($"Server killed {obj.abstractPhysicalObject.type} #{packet.ID}");
                crit.Die();
            }
        }
    }

    private void IntroduceStuff()
    {
        foreach (var packet in IntroPlayer.All()) {
            Main.Log.LogDebug($"Introduced player {packet.ID}");

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
                p.firstChunk.pos = Vec.Lerp(p.firstChunk.pos, packet.HeadPos, 0.8f);
                p.firstChunk.vel = packet.HeadVel;
                p.bodyChunks[1].pos = Vec.Lerp(p.bodyChunks[1].pos, packet.ButtPos, 0.8f);
                p.bodyChunks[1].vel = packet.ButtVel;

                p.standing = packet.Standing;
                p.bodyMode = (Player.BodyModeIndex)packet.BodyMode;
                p.animation = (Player.AnimationIndex)packet.Animation;

                session.LastInput[packet.ID] = new(packet.InputDir0, packet.InputBitmask0);

                // Don't set own inputs
                if (packet.ID != session.ClientPid) {
                    p.input[0] = new Input(packet.InputDir0, packet.InputBitmask0).ToPackage();
                    p.input[1] = new Input(packet.InputDir1, packet.InputBitmask1).ToPackage();
                    p.input[2] = new Input(packet.InputDir2, packet.InputBitmask2).ToPackage();
                    p.input[3] = new Input(packet.InputDir3, packet.InputBitmask3).ToPackage();
                    p.input[4] = new Input(packet.InputDir4, packet.InputBitmask4).ToPackage();
                    p.input[5] = new Input(packet.InputDir5, packet.InputBitmask5).ToPackage();
                    p.input[6] = new Input(packet.InputDir6, packet.InputBitmask6).ToPackage();
                    p.input[7] = new Input(packet.InputDir7, packet.InputBitmask7).ToPackage();
                    p.input[8] = new Input(packet.InputDir8, packet.InputBitmask8).ToPackage();
                    p.input[9] = new Input(packet.InputDir9, packet.InputBitmask9).ToPackage();
                }
            }
        }
    }
}
