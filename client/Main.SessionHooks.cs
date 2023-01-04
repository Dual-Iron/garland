using Common;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;

namespace Client;

partial class Main
{
    private void SessionHooks()
    {
        // Fix SlugcatStats access
        new Hook(typeof(Player).GetMethod("get_slugcatStats"), getSlugcatStats);
        new Hook(typeof(Player).GetMethod("get_Malnourished"), getMalnourished);

        // Fix disconnections
        On.RainWorldGame.Update += ExitOnDisconnect;
        On.RainWorldGame.ExitToMenu += DisconnectOnExit;

        // Prevent errors and abnormal behavior with custom session type
        On.OverWorld.ctor += OverWorld_ctor;
        On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
        On.World.ctor += World_ctor;
        IL.RainWorldGame.Update += FixPauseAndCrash; // Custom pause menu logic
        IL.RainWorldGame.GrafUpdate += FixPause; // Custom pause menu logic

        // Decentralize RoomCamera.followAbstractCreature
        IL.RainWorldGame.ctor += RainWorldGame_ctor;
    }

    private readonly Func<Func<Player, SlugcatStats>, Player, SlugcatStats> getSlugcatStats = (orig, self) => {
        return self.Data()?.stats ?? orig(self);
    };

    private readonly Func<Func<Player, bool>, Player, bool> getMalnourished = (orig, self) => self.slugcatStats.malnourished;

    private void ExitOnDisconnect(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        if (self.session is ClientSession && ClientState != ConnectionState.Connected && self.manager.upcomingProcess == null) {
            self.ExitToMenu();
        }
        orig(self);
    }

    private void DisconnectOnExit(On.RainWorldGame.orig_ExitToMenu orig, RainWorldGame self)
    {
        if (self.session is ClientSession) {
            StopClient();
        }
        orig(self);
    }

    private void OverWorld_ctor(On.OverWorld.orig_ctor orig, OverWorld self, RainWorldGame game)
    {
        if (startPacket is EnterSession session) {
            game.session = new ClientSession(session.SlugcatWorld, session.ClientPid, game);
            game.startingRoom = session.StartingRoom;
        }

        orig(self, game);
    }

    private void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
    {
        if (self.game?.session is not ClientSession || !startPacket.HasValue) {
            orig(self);
            return;
        }

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

        self.LoadWorld(startingRegion, startPacket.Value.SlugcatWorld, false);
        self.FIRSTROOM = startingRoom;
    }

    private void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
    {
        if (game?.session is not ClientSession) {
            orig(self, game, region, name, singleRoomWorld);
            return;
        }

        // Assumes Session is either IsStorySession or IsArenaSession, so pretend game is null.
        orig(self, null, region, name, singleRoomWorld);

        self.game = game;
        self.rainCycle = new(self, 10) { storyMode = true };
    }

    // TODO make follow assigned slugcat
    private void FixPauseAndCrash(ILContext il)
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

        // Realize rooms with custom logic
        cursor.GotoNext(MoveType.Before, i => i.MatchLdfld<RainWorldGame>("roomRealizer"));
        cursor.EmitDelegate(RoomRealizerHook);

        static RainWorldGame RoomRealizerHook(RainWorldGame game)
        {
            if (game.session is ClientSession session) {
                session.RoomRealizer.Update();
            }
            return game;
        }

        // Load rooms like it's a story session (even though it's not)
        cursor.GotoNext(MoveType.After, i => i.MatchCall<RainWorldGame>("get_IsStorySession"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(UpdateRoomsForStorySession);

        static bool UpdateRoomsForStorySession(bool orig, RainWorldGame game)
        {
            return orig || game.session is ClientSession;
        }
    }

    private void FixPause(ILContext il)
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

    private void RainWorldGame_ctor(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.Index = cursor.Body.Instructions.Count - 1;

        // Don't initialize RoomRealizer. Room realization logic must be redone.
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld<World>("singleRoomWorld"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(ShouldNotInitRoomRealizer);

        static bool ShouldNotInitRoomRealizer(bool orig, RainWorldGame game)
        {
            return orig || game.session is ClientSession;
        }

        // Remove check that throws an exception if no creatures are found for followAbstractCreature
        cursor.GotoPrev(MoveType.After, i => i.MatchLdfld<RoomCamera>("followAbstractCreature"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(IsFollowAbstractCreatureNull);

        static bool IsFollowAbstractCreatureNull(AbstractCreature orig, RainWorldGame game)
        {
            return orig != null || game.session is ClientSession;
        }
    }
}