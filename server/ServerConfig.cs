namespace Server;

static class ServerConfig
{
    public const string StartingRoom = "SU_A40";
    public const byte SlugcatWorld = 0; // 0=survivor, 1=monk, 2=hunter
    public const ushort CycleTimeSecondsMin = ushort.MaxValue;
    public const ushort CycleTimeSecondsMax = ushort.MaxValue;
    // TODO: implement an actual config
}
