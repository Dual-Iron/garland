using Common;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using UnityEngine;

namespace Server;

partial class Main
{
    private void SessionHooks()
    {
        // Fix SlugcatStats access
        new Hook(typeof(Player).GetMethod("get_slugcatStats"), getSlugcatStats);
        new Hook(typeof(Player).GetMethod("get_Malnourished"), getMalnourished);

        // Fix rain
        new Hook(typeof(RainCycle).GetMethod("get_RainApproaching"), getRainApproaching);

        // Fix SlugcatWorld
        On.RoomSettings.ctor += RoomSettings_ctor;

        // Just debug stuff
        On.ProcessManager.SwitchMainProcess += ProcessManager_SwitchMainProcess;

        // Jump right into the game immediately (because lobbies aren't implemented)
        On.RainWorld.LoadSetupValues += RainWorld_LoadSetupValues;

        // Prevent errors and abnormal behavior with custom session type
        On.OverWorld.ctor += OverWorld_ctor;
        On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
        On.World.ctor += World_ctor;
        IL.RainWorldGame.Update += RainWorldGame_Update;

        // Decentralize RoomCamera.followAbstractCreature
        IL.RainWorldGame.ctor += RainWorldGame_ctor;
    }

    private readonly Func<Func<Player, SlugcatStats>, Player, SlugcatStats?> getSlugcatStats = (orig, self) => self.Data()?.stats;

    private readonly Func<Func<Player, bool>, Player, bool> getMalnourished = (orig, self) => self.slugcatStats.malnourished;

    private readonly Func<Func<RainCycle, float>, RainCycle, float> getRainApproaching = (orig, self) => Mathf.InverseLerp(0f, 2400f, self.TimeUntilRain);

    private void RoomSettings_ctor(On.RoomSettings.orig_ctor orig, RoomSettings self, string name, Region region, bool template, bool firstTemplate, int playerChar)
    {
        orig(self, name, region, template, firstTemplate, ServerConfig.SlugcatWorld);
    }

    private void ProcessManager_SwitchMainProcess(On.ProcessManager.orig_SwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        orig(self, ID);

        Log.LogDebug($"Switched process to {ID}");
    }

    private RainWorldGame.SetupValues RainWorld_LoadSetupValues(On.RainWorld.orig_LoadSetupValues orig, bool distributionBuild)
    {
        return orig(distributionBuild) with { startScreen = false, playMusic = false };
    }

    private void OverWorld_ctor(On.OverWorld.orig_ctor orig, OverWorld self, RainWorldGame game)
    {
        game.session = new ServerSession(ServerConfig.SlugcatWorld, game);
        game.startingRoom = ServerConfig.StartingRoom;

        orig(self, game);
    }

    private void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
    {
        string startingRoom = self.game.startingRoom;
        string[] split = startingRoom.Split('_');
        if (split.Length < 2) {
            throw new InvalidOperationException($"Starting room is invalid: {startingRoom}");
        }
        string startingRegion = split[0];

        if (Utils.DirExistsAt(Custom.RootFolderDirectory(), "World", "Regions", startingRegion)) { }
        else if (split.Length > 2 && Utils.DirExistsAt(Custom.RootFolderDirectory(), "World", "Regions", split[1]))
            startingRegion = split[1];
        else
            throw new InvalidOperationException($"Starting room has no matching region: {startingRoom}");

        self.LoadWorld(startingRegion, ServerConfig.SlugcatWorld, false);
        self.FIRSTROOM = startingRoom;
    }

    private void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
    {
        orig(self, null, region, name, singleRoomWorld);

        self.game = game;

        if (game != null) {
            self.rainCycle = new(self, Mathf.Lerp(ServerConfig.CycleTimeSecondsMin / 60f, ServerConfig.CycleTimeSecondsMax / 60f, UnityEngine.Random.value));
        }
    }

    private void RainWorldGame_Update(ILContext il)
    {
        ILCursor cursor = new(il);

        // Realize rooms with custom logic
        cursor.GotoNext(MoveType.Before, i => i.MatchLdfld<RainWorldGame>("roomRealizer"));
        cursor.EmitDelegate(RoomRealizerHook);

        static RainWorldGame RoomRealizerHook(RainWorldGame game)
        {
            if (game.session is ServerSession session) {
                session.RoomRealizer.Update();
            }
            return game;
        }

        // Update rooms like it's a story session (even though it's not)
        cursor.GotoNext(MoveType.After, i => i.MatchCall<RainWorldGame>("get_IsStorySession"));
        cursor.Emit(OpCodes.Pop);
        cursor.Emit(OpCodes.Ldc_I4_1);
    }

    private void RainWorldGame_ctor(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.Index = cursor.Body.Instructions.Count - 1;

        // Don't initialize RoomRealizer. Room realization logic must be redone.
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld<World>("singleRoomWorld"));
        cursor.Emit(OpCodes.Pop);
        cursor.Emit(OpCodes.Ldc_I4_1);

        // Remove check that throws an exception if no creatures are found for followAbstractCreature
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld<RoomCamera>("followAbstractCreature"));
        cursor.Emit(OpCodes.Pop);
        cursor.Emit(OpCodes.Ldc_I4_1);
    }
}
