using Common;
using static Creature.Grasp.Shareability;

namespace Server;

partial class Main
{
    private void ObjectHooks()
    {
        // Sync creature deaths
        On.Creature.Die += Creature_Die;

        // Sync creature grasps, also player grabs
        On.Creature.Grab += Creature_Grab;
        On.Creature.Grasp.Release += Grasp_Release;

        // Check for player input packets and use them
        On.Room.Update += Room_Update;
        On.RWInput.PlayerInput += RWInput_PlayerInput;

        // Allow omnivorous players to eat meat
        On.Player.CanEatMeat += Player_CanEatMeat;

        // Sync client players
        On.Player.Update += Player_Update;

        // Player related fixes (player grabs, player height, array accesses).
        On.Player.Grabability += Player_Grabability;
        On.Player.MovementUpdate += Player_MovementUpdate;
        On.Player.ctor += Player_ctor;
        On.Player.checkInput += Player_checkInput;
    }

    private void Creature_Die(On.Creature.orig_Die orig, Creature self)
    {
        if (!self.dead) {
            self.BroadcastRelevant(new KillCreature(self.ID()));
        }
        orig(self);
    }

    private bool Creature_Grab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
    {
        if (self is Player && obj is Player p) {
            pacifying = p.Stunned;
        }

        if (orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying)) {
            byte bitmask = Grab.ToBitmask(shareability == NonExclusive, shareability == CanOnlyShareWithNonExclusive, overrideEquallyDominant, pacifying);

            self.BroadcastRelevant(new Grab(obj.ID(), self.ID(), dominance, (byte)graspUsed, (byte)chunkGrabbed, bitmask));

            return true;
        }
        return false;
    }

    private void Grasp_Release(On.Creature.Grasp.orig_Release orig, Creature.Grasp self)
    {
        if (!self.discontinued) {
            self.grabber.BroadcastRelevant(new Release(self.grabbed.ID(), self.grabber.ID(), (byte)self.graspUsed));
        }

        orig(self);
    }

    private void Room_Update(On.Room.orig_Update orig, Room self)
    {
        // Check for input before players update
        while (Input.Queue.Dequeue(out var sender, out var packet)) {
            if (self.game.Session().GetPlayer(sender) is AbstractCreature player) {
                self.game.Session().LastInput[player.ID()] = packet; 
            }
        }
        orig(self);
    }

    private Player.InputPackage RWInput_PlayerInput(On.RWInput.orig_PlayerInput orig, int playerNumber, RainWorld rainWorld)
    {
        if (rainWorld.processManager.currentMainLoop is RainWorldGame g && playerNumber >= 0 && playerNumber < g.Session().LastInput.Count) {
            return g.Session().LastInput[playerNumber].ToPackage();
        }
        return default;
    }

    private bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
    {
        return orig(self, crit) || self.Data()?.EatsMeat == true;
    }

    private void Player_Update(On.Player.orig_Update orig, Player p, bool eu)
    {
        orig(p, eu);

        UpdatePlayer update = new() {
            ID = p.ID(),
            Standing = p.standing,
            BodyMode = p.bodyMode.value,
            Animation = p.animation.value,
            AnimationFrame = (byte)p.animationFrame,
            FlipDirection = (sbyte)p.flipDirection,
            FlipDirectionLast = (sbyte)p.lastFlipDirection,
            HeadPos = p.firstChunk.pos,
            HeadVel = p.firstChunk.vel,
            ButtPos = p.bodyChunks[1].pos,
            ButtVel = p.bodyChunks[1].vel,
            InputDir0 = p.input[0].ToPacket().Dir,
            InputDir1 = p.input[1].ToPacket().Dir,
            InputDir2 = p.input[2].ToPacket().Dir,
            InputDir3 = p.input[3].ToPacket().Dir,
            InputDir4 = p.input[4].ToPacket().Dir,
            InputDir5 = p.input[5].ToPacket().Dir,
            InputDir6 = p.input[6].ToPacket().Dir,
            InputDir7 = p.input[7].ToPacket().Dir,
            InputDir8 = p.input[8].ToPacket().Dir,
            InputDir9 = p.input[9].ToPacket().Dir,
            InputBitmask0 = p.input[0].ToPacket().Bitmask,
            InputBitmask1 = p.input[1].ToPacket().Bitmask,
            InputBitmask2 = p.input[2].ToPacket().Bitmask,
            InputBitmask3 = p.input[3].ToPacket().Bitmask,
            InputBitmask4 = p.input[4].ToPacket().Bitmask,
            InputBitmask5 = p.input[5].ToPacket().Bitmask,
            InputBitmask6 = p.input[6].ToPacket().Bitmask,
            InputBitmask7 = p.input[7].ToPacket().Bitmask,
            InputBitmask8 = p.input[8].ToPacket().Bitmask,
            InputBitmask9 = p.input[9].ToPacket().Bitmask,
        };
        p.BroadcastRelevant(update, LiteNetLib.DeliveryMethod.ReliableSequenced);
    }

    private Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (self != obj && obj is Player) {
            return Player.ObjectGrabability.BigOneHand;
        }
        return orig(self, obj);
    }

    private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        orig(self, eu);

        if (self.bodyChunkConnections[0].distance == 17 && self.Data() is SharedPlayerData data) {
            float tallModifier = data.Fat * 4;
            float cuteModifier = data.Charm * (data.Charm < 0 ? 2 : 6);

            // Fat makes you shorter or taller all the same.
            // Charm makes you significantly shorter, or slightly taller.
            self.bodyChunkConnections[0].distance += tallModifier - cuteModifier;
        }
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature p, World world)
    {
        int num = p.PlayerState().playerNumber;
        p.PlayerState().playerNumber = 0;
        try { orig(self, p, world); }
        finally { p.PlayerState().playerNumber = num; }
    }

    private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        // Prevent accessing Options array with num > 3 by setting it to a temporary.
        int num = self.playerState.playerNumber;
        if (self.stun != 0 || self.dead) {
            self.playerState.playerNumber = 0;
        }
        try { orig(self); }
        finally { self.playerState.playerNumber = num; }
    }
}
