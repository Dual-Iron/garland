using BepInEx;
using System;
using System.Security.Permissions;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Client;

[BepInPlugin("org.ozql.garland", "Garland", "0.1.0")]
sealed partial class Plugin : BaseUnityPlugin
{
    public void OnEnable()
    {
        try {
            Main.Instance.Hook();
        }
        catch (Exception e) {
            Main.Log.LogError(e);
        }
    }

    public void OnDisable()
    {
        Main.Instance.StopClient();
    }

    public void OnApplicationQuit()
    {
        Main.Instance.StopClient();
    }
}
