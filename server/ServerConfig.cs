namespace Server;

static class ServerConfig
{
    public static string StartingRoom = "SU_A40";
    public static byte SlugcatWorld = 0; // 0=survivor, 1=monk, 2=hunter
    public static float CycleTimeMinutesMin = 400f / 60f;
    public static float CycleTimeMinutesMax = 800f / 60f;
    // TODO: implement an actual config
}
