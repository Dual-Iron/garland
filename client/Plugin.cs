using BepInEx;
using BepInEx.Logging;
using Common;
using LiteNetLib;
using System;
using System.Threading;

namespace Client;

[BepInPlugin("org.ozql.garland", "Garland", "0.1.0")]
sealed class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger { get; private set; }

    static NetManager client;

    public void OnEnable()
    {
        Logger = base.Logger;

        EventBasedNetListener listener = new();
        client = new(listener);
        client.Start();
        client.Connect("localhost", Variables.Port, Variables.ConnectionKey);
        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
        {
            Console.WriteLine($"We got: {dataReader.GetString(100)}");
            dataReader.Recycle();
        };

        try {
            On.RainWorld.Update += RainWorld_Update;
        }
        catch (Exception e) {
            Logger.LogError(e);
        }
    }

    private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    {
        orig(self);

        try {
            client.PollEvents();
        }
        catch (Exception e) {
            Logger.LogError(e);
        }
    }
}
