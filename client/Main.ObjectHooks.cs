using Common;
using RWCustom;
using UnityEngine;

namespace Client;

partial class Main
{
    private void ObjectHooks()
    {
        // Set chunk positions etc. Pre- and post-update stuff.
        On.RainWorldGame.Update += UpdateState;

        // Send input to server, and set input for other players
        On.Player.checkInput += Player_checkInput;
        On.RWInput.PlayerInput += RWInput_PlayerInput;

        // Allow omnivorous players to eat meat
        On.Player.CanEatMeat += Player_CanEatMeat;

        // Set player colors and The Mark
        On.PlayerGraphics.Update += PlayerGraphics_Update;
        On.PlayerGraphics.SlugcatColor += PlayerGraphics_SlugcatColor;
    }

    private void UpdateState(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        if (self.session is not ClientSession sess) return;

        foreach (var kvp in sess.UpdatePlayer) {
            if (sess.Objects.TryGetValue(kvp.Key, out var obj) && obj is Player p) {
                var packet = kvp.Value;

                p.firstChunk.pos = Vector2.Lerp(p.firstChunk.pos, packet.HeadPos, 0.8f);
                p.firstChunk.vel = packet.HeadVel;
                p.bodyChunks[1].pos = Vector2.Lerp(p.bodyChunks[1].pos, packet.ButtPos, 0.8f);
                p.bodyChunks[1].vel = packet.ButtVel;

                p.standing = packet.Standing;
                p.bodyMode = (Player.BodyModeIndex)packet.BodyMode;
                p.animation = (Player.AnimationIndex)packet.Animation;
                p.animationFrame = packet.AnimationFrame;
            }
        }
    }

    private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        if (self.Game().session is not ClientSession session) {
            orig(self);
            return;
        }

        // If this is us, then send our input off to the server!
        if (self.IsMyPlayer()) {
            var package = RWInput.PlayerInput(playerNumber: 0, self.room.game.rainWorld.options, self.room.game.setupValues);

            ServerPeer?.Send(package.ToPacket(), LiteNetLib.DeliveryMethod.ReliableSequenced);
        }

        orig(self);

        if (self.IsMyPlayer()) {
            return;
        }

        // Set input according to what server says.
        if (session.UpdatePlayer.TryGetValue(self.ID(), out var packet)) {
            self.input[0] = new Common.Input(packet.InputDir0, packet.InputBitmask0).ToPackage();
            self.input[1] = new Common.Input(packet.InputDir1, packet.InputBitmask1).ToPackage();
            self.input[2] = new Common.Input(packet.InputDir2, packet.InputBitmask2).ToPackage();
            self.input[3] = new Common.Input(packet.InputDir3, packet.InputBitmask3).ToPackage();
            self.input[4] = new Common.Input(packet.InputDir4, packet.InputBitmask4).ToPackage();
            self.input[5] = new Common.Input(packet.InputDir5, packet.InputBitmask5).ToPackage();
            self.input[6] = new Common.Input(packet.InputDir6, packet.InputBitmask6).ToPackage();
            self.input[7] = new Common.Input(packet.InputDir7, packet.InputBitmask7).ToPackage();
            self.input[8] = new Common.Input(packet.InputDir8, packet.InputBitmask8).ToPackage();
            self.input[9] = new Common.Input(packet.InputDir9, packet.InputBitmask9).ToPackage();
        }
        // Or stand still.
        else if (session.PlayerLastInput.TryGetValue(self.ID(), out var input)) {
            self.input[0] = input.ToPackage();
        }
    }
    private Player.InputPackage RWInput_PlayerInput(On.RWInput.orig_PlayerInput orig, int playerNumber, Options options, RainWorldGame.SetupValues setup)
    {
        if (Utils.Rw.processManager.currentMainLoop is RainWorldGame game && game.session is ClientSession) {
            // Just take player 0's input. The other players aren't really there.
            playerNumber = 0;
        }
        return orig(playerNumber, options, setup);
    }

    private bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
    {
        return orig(self, crit) || self.Data()?.EatsMeat == true;
    }

    private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        float markAlpha = self.markAlpha;
        orig(self);
        if (self.player.Data() is SharedPlayerData data && data.HasMark) {
            float alpha = Mathf.InverseLerp(30f, 80f, self.player.touchedNoInputCounter) - Random.value * Mathf.InverseLerp(80f, 30f, self.player.touchedNoInputCounter);

            self.markAlpha = Custom.LerpAndTick(markAlpha, Mathf.Clamp(alpha, 0f, 1f) * self.markBaseAlpha, 0.1f, 1/30f);
        }
    }

    private Color PlayerGraphics_SlugcatColor(On.PlayerGraphics.orig_SlugcatColor orig, int i)
    {
        if (Utils.Rw.processManager.currentMainLoop is RainWorldGame game && game.session is ClientSession sess && sess.ClientData.TryGetValue(i, out var data)) {
            return data.SkinColor;
        }
        return orig(i);
    }
}
