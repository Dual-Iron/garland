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

        // Jump right into the game immediately (because lobbies aren't implemented)
        On.RainWorld.LoadSetupValues += RainWorld_LoadSetupValues;

        // Do not
        On.RainWorldGame.GameOver += delegate { };

        // Prevent errors and abnormal behavior with custom session type
        On.OverWorld.ctor += OverWorld_ctor;
        On.World.ctor += World_ctor;
        IL.RainWorldGame.Update += RainWorldGame_Update;

        // Decentralize RoomCamera.followAbstractCreature
        IL.RainWorldGame.ctor += RainWorldGame_ctor;
    }

    private readonly Func<Func<Player, SlugcatStats>, Player, SlugcatStats> getSlugcatStats = (orig, self) => self.Data()?.Stats() ?? orig(self);

    private readonly Func<Func<Player, bool>, Player, bool> getMalnourished = (orig, self) => self.slugcatStats.malnourished;

    private readonly Func<Func<RainCycle, float>, RainCycle, float> getRainApproaching = (orig, self) => Mathf.InverseLerp(0f, 2400f, self.TimeUntilRain);

    private RainWorldGame.SetupValues RainWorld_LoadSetupValues(On.RainWorld.orig_LoadSetupValues orig, bool distributionBuild)
    {
        return orig(distributionBuild) with { startScreen = false, playMusic = false };
    }

    private void OverWorld_ctor(On.OverWorld.orig_ctor orig, OverWorld self, RainWorldGame game)
    {
        game.session = new ServerSession(new(ServerConfig.SlugcatWorld), game);
        game.startingRoom = ServerConfig.StartingRoom;

        orig(self, game);
    }

    private void World_ctor(On.World.orig_ctor orig, World self, RainWorldGame game, Region region, string name, bool singleRoomWorld)
    {
        orig(self, null, region, name, singleRoomWorld);

        self.game = game;

        if (game != null) {
            int seconds = UnityEngine.Random.Range(ServerConfig.CycleTimeSecondsMin, ServerConfig.CycleTimeSecondsMax + 1);

            self.rainCycle = new(self, 10) { cycleLength = seconds * 40 };

            TimeSpan time = TimeSpan.FromSeconds(seconds);
            if ((int)time.TotalHours > 0) {
                Log.LogDebug($"Cycle length is {(int)time.TotalHours} hours, {time.Minutes} minutes, and {time.Seconds} seconds");
            }
            else if ((int)time.TotalMinutes > 0) {
                Log.LogDebug($"Cycle length is {(int)time.TotalMinutes} minutes and {time.Seconds} seconds");
            }
            else {
                Log.LogDebug($"Cycle length is {(int)time.TotalSeconds} seconds");
            }
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

        cursor.GotoNext(MoveType.After, i => i.MatchCall<RainWorldGame>("get_IsStorySession"));
        cursor.GotoNext(MoveType.After, i => i.MatchCall<RainWorldGame>("get_IsStorySession"));
        cursor.Emit(OpCodes.Pop);
        cursor.Emit(OpCodes.Ldc_I4_0);

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
