using System.IO;

namespace Common;

public static class Utils
{
    public const string ConnectionKey = "GARLAND-0"; // increment with each major version
    public const int MaxConnections = 55; // arbitrary
    public const int DefaultPort = 10933;

    private static RainWorld? rw;
    public static RainWorld Rw => rw ??= UnityEngine.Object.FindObjectOfType<RainWorld>();

    public static bool DirExistsAt(params string[] path)
    {
        return Directory.Exists(BepInEx.Utility.CombinePaths(path));
    }

    public static RainWorldGame Game(this PhysicalObject o) => o.abstractPhysicalObject.world.game;

    public static PlayerState PlayerState(this AbstractCreature creature)
    {
        return (PlayerState)creature.state;
    }

    public static PlayerGraphics Graphics(this Player p)
    {
        return (PlayerGraphics)p.graphicsModule;
    }

    public static float SeededRandom(int seed)
    {
        int seed2 = UnityEngine.Random.seed;
        UnityEngine.Random.seed = seed;
        float value = UnityEngine.Random.value;
        UnityEngine.Random.seed = seed2;
        return value;
    }
}
