namespace Client;

sealed class ClientSession : GameSession
{
    public ClientSession(byte slugcatWorld, RainWorldGame game) : base(game)
    {
        SlugcatWorld = slugcatWorld;
    }

    public byte SlugcatWorld { get; }
}
