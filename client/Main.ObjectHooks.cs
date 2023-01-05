using Common;
using Vec = UnityEngine.Vector2;

namespace Client;

partial class Main
{
    private void ObjectHooks()
    {
        On.RainWorldGame.Update += UpdateState;

        // Send input to server
        On.Player.checkInput += Player_checkInput;
        On.RWInput.PlayerInput += RWInput_PlayerInput;
    }

    private void UpdateState(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        if (self.session is not ClientSession sess) return;

        foreach (var kvp in sess.UpdatePlayerCache) {
            if (sess.Objects.TryGetValue(kvp.Key, out var obj) && obj is Player p) {
                var packet = kvp.Value;

                p.firstChunk.pos = Vec.Lerp(p.firstChunk.pos, packet.HeadPos, 0.8f);
                p.firstChunk.vel = packet.HeadVel;
                p.bodyChunks[1].pos = Vec.Lerp(p.bodyChunks[1].pos, packet.ButtPos, 0.8f);
                p.bodyChunks[1].vel = packet.ButtVel;

                p.standing = packet.Standing;
                p.bodyMode = (Player.BodyModeIndex)packet.BodyMode;
                p.animation = (Player.AnimationIndex)packet.Animation;
            }
        }
        sess.UpdatePlayerCache.Clear();
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

        // If this isn't us, then set all our input as the server says.
        if (self.IsMyPlayer()) {
            return;
        }

        if (session.UpdatePlayerCache.TryGetValue(self.Pid(), out var packet)) {
            self.input[0] = new Input(packet.InputDir0, packet.InputBitmask0).ToPackage();
            self.input[1] = new Input(packet.InputDir1, packet.InputBitmask1).ToPackage();
            self.input[2] = new Input(packet.InputDir2, packet.InputBitmask2).ToPackage();
            self.input[3] = new Input(packet.InputDir3, packet.InputBitmask3).ToPackage();
            self.input[4] = new Input(packet.InputDir4, packet.InputBitmask4).ToPackage();
            self.input[5] = new Input(packet.InputDir5, packet.InputBitmask5).ToPackage();
            self.input[6] = new Input(packet.InputDir6, packet.InputBitmask6).ToPackage();
            self.input[7] = new Input(packet.InputDir7, packet.InputBitmask7).ToPackage();
            self.input[8] = new Input(packet.InputDir8, packet.InputBitmask8).ToPackage();
            self.input[9] = new Input(packet.InputDir9, packet.InputBitmask9).ToPackage();
        }
        // Or stand still.
        else if (session.LastInput.TryGetValue(self.Pid(), out var input)) {
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
}
