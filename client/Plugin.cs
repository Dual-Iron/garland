using BepInEx;
using BepInEx.Logging;
using System;

namespace Client;

[BepInPlugin("org.ozql.garland", "Garland", "0.1.0")]
sealed class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger { get; private set; }

    public void OnEnable()
    {
        Logger = base.Logger;

        try {
            // Hooks go here
        }
        catch (Exception e) {
            Logger.LogError(e);
        }
    }
}
