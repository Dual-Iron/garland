using Common;
using RWCustom;
using UnityEngine;

namespace Client;

partial class Main
{
    private void ObjectHooks()
    {
        // Fix player grabs
        On.Creature.Grab += Creature_Grab;
        On.Creature.ReleaseGrasp += Creature_ReleaseGrasp;
        On.Player.Grabability += Player_Grabability;

        // Set chunk positions etc. Pre- and post-update stuff.
        On.RainWorldGame.Update += UpdateState;

        // Send input to server, set input for other players, and fix playerNumber-related crash
        On.Player.ctor += Player_ctor;
        On.Player.checkInput += Player_checkInput;
        On.RWInput.PlayerInput += RWInput_PlayerInput;

        // Allow omnivorous players to eat meat
        On.Player.CanEatMeat += Player_CanEatMeat;

        // Set player graphics and The Mark
        On.Player.MovementUpdate += Player_MovementUpdate;
        On.PlayerGraphics.ctor += PlayerGraphics_ctor;
        On.PlayerGraphics.Update += PlayerGraphics_Update;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        On.PlayerGraphics.SlugcatColor += PlayerGraphics_SlugcatColor;
        On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
        On.MiniFly.ViableForBuzzaround += MiniFly_ViableForBuzzaround;
    }

    public static bool GrabPacket = false;
    private bool Creature_Grab(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
    {
        if (!GrabPacket) return false;
        return orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
    }
    private void Creature_ReleaseGrasp(On.Creature.orig_ReleaseGrasp orig, Creature self, int grasp)
    {
        if (GrabPacket) orig(self, grasp);
    }

    private int Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (self != obj && obj is Player) {
            return (int)Player.ObjectGrabability.BigOneHand;
        }
        return orig(self, obj);
    }

    private void UpdateState(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig(self);

        if (self.session is not ClientSession sess) return;

        foreach (var kvp in sess.UpdatePlayer) {
            if (!sess.RoomRealizer.TryFind(kvp.Key, out Player p)) {
                continue;
            }

            var packet = kvp.Value;

            p.firstChunk.pos = Vector2.Lerp(p.firstChunk.pos, packet.HeadPos, 0.8f);
            p.firstChunk.vel = packet.HeadVel;
            p.bodyChunks[1].pos = Vector2.Lerp(p.bodyChunks[1].pos, packet.ButtPos, 0.8f);
            p.bodyChunks[1].vel = packet.ButtVel;

            p.standing = packet.Standing;
            p.bodyMode = (Player.BodyModeIndex)packet.BodyMode;
            p.animation = (Player.AnimationIndex)packet.Animation;
            p.animationFrame = packet.AnimationFrame;
            p.flipDirection = packet.FlipDirection;
            p.lastFlipDirection = packet.FlipDirectionLast;
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
        if (self.Game().session is not ClientSession session) {
            orig(self);
            return;
        }

        // If this is us, then send our input off to the server!
        if (self.IsMyPlayer() && session.game.pauseMenu == null) {
            var package = RWInput.PlayerInput(playerNumber: 0, self.room.game.rainWorld.options, self.room.game.setupValues);

            ServerPeer?.Send(package.ToPacket(), LiteNetLib.DeliveryMethod.ReliableSequenced);
        }

        // Prevent accessing Options array with num > 3 by setting it to a temporary.
        int num = self.playerState.playerNumber;
        if (self.stun != 0 || self.dead) {
            self.playerState.playerNumber = 0;
        }
        try { orig(self); }
        finally { self.playerState.playerNumber = num; }

        if (session.game.pauseMenu != null) {
            self.input[0] = default;
            self.mapInput = default;
        }

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

    // Player graphics and stuff
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

    private void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
    {
        orig(self, ow);

        if (self.player.Data() is SharedPlayerData data)
            foreach (var tailSeg in self.tail) {
                tailSeg.rad *= Custom.LerpMap(data.Fat * 0.65f - data.Speed * 0.35f, -1, +1, 0.6f, 1.4f);
            }
    }

    private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        float markAlpha = self.markAlpha;

        orig(self);

        if (self.player.Data() is not SharedPlayerData data) {
            return;
        }

        // Increase or decrease breathing speed by 50% * speed
        self.breath += (self.breath - self.lastBreath) * data.Speed * 0.5f;

        // If Speed < 0, make player look at objects for longer.
        if (Random.value < data.Speed * -1 / 80) {
            self.objectLooker.timeLookingAtThis = 0;
        }

        // Saint eyes :)
        if (data.Charm > 0.66f) {
            self.blink = 5;
        }
        // Twitch occasionally, for good measure. Only occurs at negative charm.
        else if (Random.value < data.Charm * -1 / 80) {
            self.NudgeDrawPosition(Random.value < 0.5f ? 0 : 1, Custom.RNV() * (2 + 2 * Random.value));
        }

        if (data.HasMark) {
            float alpha = Mathf.InverseLerp(30f, 80f, self.player.touchedNoInputCounter) - Random.value * Mathf.InverseLerp(80f, 30f, self.player.touchedNoInputCounter);
            self.markAlpha = Custom.LerpAndTick(markAlpha, Mathf.Clamp(alpha, 0f, 1f) * self.markBaseAlpha, 0.1f, 1 / 30f);
        }
    }

    private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        float fat = self.player.Data()?.Fat ?? 0;
        sLeaser.sprites[0].scaleX += fat * 0.15f;
        sLeaser.sprites[1].scaleX += fat * 0.08f;

        float charm = self.player.Data()?.Charm ?? 0;
        if (charm < -0.66f) {
            // If they're really ugly, make them *really* ugly by scaling the face up slightly.
            if (sLeaser.sprites[9].scaleX is 1 or -1) sLeaser.sprites[9].scaleX -= charm * 0.1f;
            if (sLeaser.sprites[9].scaleY is 1 or -1) sLeaser.sprites[9].scaleY -= charm * 0.1f;
        }
    }

    private Color PlayerGraphics_SlugcatColor(On.PlayerGraphics.orig_SlugcatColor orig, int i)
    {
        if (Utils.Rw.processManager.currentMainLoop is RainWorldGame game && game.session is ClientSession sess && sess.ClientData.TryGetValue(i, out var data)) {
            return data.SkinColor;
        }
        return orig(i);
    }

    private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);

        Color bodyColor = PlayerGraphics.SlugcatColor(self.player.playerState.slugcatCharacter);
        Color eyeColor = self.player.Data()?.EyeColor ?? palette.blackColor;
        if (self.malnourished > 0f) {
            float num = (!self.player.Malnourished) ? Mathf.Max(0f, self.malnourished - 0.005f) : self.malnourished;
            bodyColor = Color.Lerp(bodyColor, Color.gray, 0.4f * num);
            eyeColor = Color.Lerp(eyeColor, Color.Lerp(Color.white, palette.fogColor, 0.5f), 0.2f * num * num);
        }
        for (int i = 0; i < sLeaser.sprites.Length; i++) {
            sLeaser.sprites[i].color = bodyColor;
        }
        sLeaser.sprites[9].color = eyeColor;
    }

    // Make flies buzz around gross, fat players
    private bool MiniFly_ViableForBuzzaround(On.MiniFly.orig_ViableForBuzzaround orig, MiniFly self, AbstractCreature crit)
    {
        if (crit.state.alive && crit.realizedCreature is Player p && p.Data() is SharedPlayerData data && data.Charm < 0 && data.Charm + 1 < data.Fat) {
            crit.state.alive = false;
            try { return orig(self, crit); }
            finally { crit.state.alive = true; }
        }
        else {
            return orig(self, crit);
        }
    }
}
