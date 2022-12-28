namespace Client;

sealed partial class Plugin
{
    private static void SessionHooks()
    {
        On.OverWorld.ctor += OverWorld_ctor;
    }

    private static void OverWorld_ctor(On.OverWorld.orig_ctor orig, OverWorld self, RainWorldGame game)
    {
        game.startingRoom = "SU_A40";
        orig(self, game);

        Log.LogInfo(game.world);
    }
}