using Common;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using UnityEngine;

namespace Server;

sealed partial class Plugin
{
    private static void GameHooks()
    {
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

    private static void ProcessManager_SwitchMainProcess(On.ProcessManager.orig_SwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        orig(self, ID);

        Log.LogDebug($"Switched process to {ID}");
    }

    private static RainWorldGame.SetupValues RainWorld_LoadSetupValues(On.RainWorld.orig_LoadSetupValues orig, bool distributionBuild)
    {
        return orig(distributionBuild) with { startScreen = false, playMusic = false };
    }

    private static void OverWorld_ctor(On.OverWorld.orig_ctor orig, OverWorld self, RainWorldGame game)
    {
        game.session = new ServerSession(ServerConfig.SlugcatWorld, game);
        game.startingRoom = ServerConfig.StartingRoom;

        orig(self, game);
    }

    private static void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
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

    private static void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
    {
        orig(self, null, region, name, singleRoomWorld);

        self.game = game;

        if (game != null) {
            self.rainCycle = new(self, Mathf.Lerp(ServerConfig.CycleTimeMinutesMin, ServerConfig.CycleTimeMinutesMax, UnityEngine.Random.value));
        }
    }

    private static void RainWorldGame_Update(ILContext il)
    {
        ILCursor cursor = new(il);

        // Load rooms like it's a story session (even though it's not)
        cursor.GotoNext(MoveType.After, i => i.MatchCall<RainWorldGame>("get_IsStorySession"));
        cursor.Emit(OpCodes.Pop);
        cursor.Emit(OpCodes.Ldc_I4_1);
    }

    private static void RainWorldGame_ctor(ILContext il)
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
