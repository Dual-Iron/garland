using System.IO;

namespace Common;

public static class Utils
{
    public static bool DirExistsAt(params string[] path)
    {
        return Directory.Exists(BepInEx.Utility.CombinePaths(path));
    }
}
