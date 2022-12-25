using BepInEx;
using BepInEx.Logging;
using Common;
using LiteNetLib;
using LiteNetLib.Utils;
using System;

namespace Client;

[BepInPlugin("org.ozql.garland", "Garland", "0.1.0")]
sealed class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger { get; private set; }

    static NetManager client;
    static NetManager server;

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
            if (server == null) {
                StartServer();
            }
            else if (client == null) {
                StartClient();
            }
            else {
                server.PollEvents();
                client.PollEvents();
            }
        }
        catch (Exception e) {
            Logger.LogError(e);
        }
    }

    private static void StartServer()
    {
        EventBasedNetListener listener = new();
        server = new(listener) { AutoRecycle = true };
        server.Start(Variables.Port);

        Logger.LogDebug("Listening for messages");

        listener.ConnectionRequestEvent += request => {
            if (server.ConnectedPeersCount < Variables.MaxConnections)
                request.AcceptIfKey(Variables.ConnectionKey);
            else
                request.Reject();
        };

        listener.PeerConnectedEvent += peer => {
            DateTime now = DateTime.UtcNow;
            Console.WriteLine($"Connection: {peer.EndPoint} at {now:HH:mm:ss}.{now.Millisecond:D3}");

            NetDataWriter writer = new();
            writer.Put("Hello client!");
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        };
    }

    private static void StartClient()
    {
        EventBasedNetListener listener = new();
        client = new(listener) { AutoRecycle = true };
        client.Start();
        client.Connect("localhost", Variables.Port, Variables.ConnectionKey);

        Logger.LogDebug("Attempting to connect");

        listener.PeerConnectedEvent += p => {
            Logger.LogDebug("Connected");
        };
        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
            DateTime now = DateTime.UtcNow;
            Logger.LogDebug($"Received \"{dataReader.GetString(100)}\" at {now:HH:mm:ss}.{now.Millisecond:D3}");
        };
    }
}
