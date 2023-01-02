using BepInEx;
using System;
using System.Security.Permissions;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Server;

[BepInPlugin("org.ozql.garland", "Garland", "0.1.0")]
sealed partial class Plugin : BaseUnityPlugin
{
    public void OnEnable()
    {
        try {
            Main.Instance.Hook();
        }
        catch (Exception e) {
            Main.Log.LogFatal(e);
        }
    }

    public void OnApplicationQuit()
    {
        Main.Instance.server.Stop(sendDisconnectMessages: true);
    }
}
