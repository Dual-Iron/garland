using Client.Sessions;
using Common;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using UnityEngine;

namespace Client;

sealed partial class Plugin
{
    public const ProcessManager.MenuSetup.StoryGameInitCondition online = (ProcessManager.MenuSetup.StoryGameInitCondition)(-40);
    public static string? StartRoom;

    private static void SessionHooks()
    {
        // Prevent errors and abnormal behavior with custom session type
        On.OverWorld.ctor += OverWorld_ctor;
        On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
        On.World.ctor += World_ctor;
        IL.RainWorldGame.Update += RainWorldGame_Update; // Custom pause menu logic
        IL.RainWorldGame.GrafUpdate += RainWorldGame_GrafUpdate; // Custom pause menu logic

        // Decentralize RoomCamera.followAbstractCreature
        IL.RainWorldGame.ctor += RainWorldGame_ctor;

        // Fix pausing
        On.RainWorldGame.ExitToMenu += RainWorldGame_ExitToMenu;
    }

    private static void OverWorld_ctor(On.OverWorld.orig_ctor orig, OverWorld self, RainWorldGame game)
    {
        game.session = new ClientSession(game);
        game.startingRoom = StartRoom;

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

        self.LoadWorld(startingRegion, 0, false);
        self.FIRSTROOM = startingRoom;
    }

    private static void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
    {
        orig(self, null, region, name, singleRoomWorld);

        self.game = game;

        if (game != null) {
            self.rainCycle = new(self, minutes: 100);
        }
    }

    // TODO fix packet system please
    // TODO make follow assigned slugcat
    private static void RainWorldGame_Update(ILContext il)
    {
        ILCursor cursor = new(il);

        // Pretend pause menu is null
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<RainWorldGame>("pauseMenu"));
        cursor.EmitDelegate(ModifyPause);

        static Menu.PauseMenu? ModifyPause(Menu.PauseMenu pauseMenu)
        {
            // Skip vanilla pause menu logic—update both the game and the pause menu at once
            if (pauseMenu?.game.session is ClientSession) {
                pauseMenu.Update();
                pauseMenu.game.lastPauseButton = true; // prevent opening multiple pause menus at once
                foreach (RoomCamera camera in pauseMenu.game.cameras) {
                    camera.hud?.Update();
                }
                return null;
            }
            return pauseMenu;
        }

        // Load rooms like it's a story session (even though it's not)
        cursor.GotoNext(MoveType.After, i => i.MatchCall<RainWorldGame>("get_IsStorySession"));
        cursor.Emit(OpCodes.Pop);
        cursor.Emit(OpCodes.Ldc_I4_1);
    }

    private static void RainWorldGame_GrafUpdate(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<RainWorldGame>("pauseMenu"));
        cursor.Emit(OpCodes.Ldarg_1); // timeStacker
        cursor.EmitDelegate(ModifyPause);

        static Menu.PauseMenu? ModifyPause(Menu.PauseMenu pauseMenu, float timeStacker)
        {
            // Skip vanilla pause menu logic—render both the game and the pause menu at once
            if (pauseMenu?.game.session is ClientSession) {
                pauseMenu.GrafUpdate(timeStacker);
                foreach (RoomCamera camera in pauseMenu.game.cameras) {
                    camera.hud?.Draw(timeStacker);
                }
                return null;
            }
            return pauseMenu;
        }
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

    private static void RainWorldGame_ExitToMenu(On.RainWorldGame.orig_ExitToMenu orig, RainWorldGame self)
    {
        StopClient();
        orig(self);
    }
}