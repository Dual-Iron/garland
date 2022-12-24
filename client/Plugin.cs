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
            if (client == null) {
                EventBasedNetListener listener = new();
                client = new(listener) { AutoRecycle = true };
                client.Start();
                client.Connect("localhost", Variables.Port, Variables.ConnectionKey);

                listener.PeerConnectedEvent += p => {
                    Logger.LogDebug("Connected");
                };
                listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
                    DateTime now = DateTime.UtcNow;
                    Logger.LogDebug($"Received \"{dataReader.GetString(100)}\" at {now:HH:mm:ss}.{now.Millisecond:D3}");
                };

                Logger.LogDebug("Attempting to connect");
            }
            else {
                client.PollEvents();
            }
        }
        catch (Exception e) {
            Logger.LogError(e);
        }
    }
}
