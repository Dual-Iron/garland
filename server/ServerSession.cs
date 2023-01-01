namespace Server;

sealed class ServerSession : GameSession
{
    public ServerSession(byte slugcatWorld, RainWorldGame game) : base(game)
    {
        SlugcatWorld = slugcatWorld;
    }

    public byte SlugcatWorld { get; }
}
