using MonoMod.RuntimeDetour;
using System;
using Setup = RainWorldGame.SetupValues;

namespace Client;

partial class Main
{
    private void WorldHooks()
    {
        // Prevent world creatures from spawning as client
        new Hook(typeof(RainWorldGame).GetMethod("get_setupValues"), getSetupValues);

        // Prevent spawning various placed physical objects, and random objects like spears and rocks
        On.Room.ctor += Room_ctor;

        // Fix SlugcatWorld
        On.RoomSettings.ctor += RoomSettings_ctor;

        On.AbstractRoom.AddEntity += (orig, self, ent) => {
            Log.LogDebug($"{self.name,-7} added {ent}");
            orig(self, ent);
        };
    }

    private readonly Func<Func<RainWorldGame, Setup>, RainWorldGame, Setup> getSetupValues = (orig, game) => {
        if (game.session is ClientSession) {
            return orig(game) with { worldCreaturesSpawn = false };
        }
        return orig(game);
    };

    int? storyCharOverride = null;
    private void Room_ctor(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom)
    {
        if (game?.session is ClientSession session) {
            storyCharOverride = session.SlugcatWorld;
            orig(self, game, world, abstractRoom);

            // Don't spawn physical objects!!
            self.abstractRoom.firstTimeRealized = false;
        }
        else {
            storyCharOverride = null;
            orig(self, game, world, abstractRoom);
        }
    }
    private void RoomSettings_ctor(On.RoomSettings.orig_ctor orig, RoomSettings self, string name, Region region, bool template, bool firstTemplate, int playerChar)
    {
        if (storyCharOverride.HasValue) {
            playerChar = storyCharOverride.Value;
        }
        orig(self, name, region, template, firstTemplate, playerChar);
    }
}
