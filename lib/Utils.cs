using System.IO;

namespace Common;

public static class Utils
{
    private static RainWorld? rw;
    public static RainWorld Rw => rw ??= UnityEngine.Object.FindObjectOfType<RainWorld>();

    public static bool DirExistsAt(params string[] path)
    {
        return Directory.Exists(BepInEx.Utility.CombinePaths(path));
    }
}
