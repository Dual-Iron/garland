﻿using Common;

namespace Server;

partial class Main
{
    private void ObjectHooks()
    {
        On.Room.Update += Room_Update;
        On.RWInput.PlayerInput += RWInput_PlayerInput;
        On.Player.Update += Player_Update;
    }

    private void Room_Update(On.Room.orig_Update orig, Room self)
    {
        // Check for input before players update
        while (Input.Queue.Dequeue(out var sender, out var packet)) {
            if (self.game.Session().GetPlayer(sender) is AbstractCreature player) {
                self.game.Session().LastInput[player.ID.number] = packet; 
            }
        }
        orig(self);
    }

    private Player.InputPackage RWInput_PlayerInput(On.RWInput.orig_PlayerInput orig, int playerNumber, Options options, RainWorldGame.SetupValues setup)
    {
        // Return last input instead of actual controller input
        if (Utils.Rw.processManager.currentMainLoop is RainWorldGame g && playerNumber >= 0 && playerNumber < g.Session().LastInput.Count) {
            return g.Session().LastInput[playerNumber].ToPackage();
        }
        return default;
    }

    private void Player_Update(On.Player.orig_Update orig, Player p, bool eu)
    {
        orig(p, eu);

        var sess = p.Session();
        UpdatePlayer update = new() {
            ID = p.abstractPhysicalObject.ID.number,
            Standing = p.standing,
            BodyMode = (byte)p.bodyMode,
            Animation = (byte)p.animation,
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
        sess.SendObjectUpdate(p, update);
    }
}