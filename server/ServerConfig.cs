namespace Server;

static class ServerConfig
{
    public static string StartingRoom = "SU_A40";
    public static byte SlugcatWorld = 0; // 0=survivor, 1=monk, 2=hunter
    public static float CycleTimeMinutesMin = 0.1f;//400f / 60f;
    public static float CycleTimeMinutesMax = 0.2f;//800f / 60f;
    // TODO: use reasonable, non-testing variables, lol.
    // TODO: implement an actual config
}
