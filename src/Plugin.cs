using BepInEx;
using BepInEx.Logging;
using Common;
using LiteNetLib;
using LiteNetLib.Utils;
using System;

namespace Garland;

[BepInPlugin("org.ozql.garland", "Garland", "0.1.0")]
sealed partial class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger { get; private set; } = null!;
    public static ManualLogSource ClientLog { get; } = BepInEx.Logging.Logger.CreateLogSource("Client");
    public static ManualLogSource ServerLog { get; } = BepInEx.Logging.Logger.CreateLogSource("Server");

    public static NetManager? client;
    public static NetManager? server;

    public void OnEnable()
    {
        Logger = base.Logger;

        try {
            MenuHooks();
        }
        catch (Exception e) {
            Logger.LogError(e);
        }
    }

    private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    {
        orig(self);

        try {
            server?.PollEvents();
            client?.PollEvents();
        }
        catch (Exception e) {
            Logger.LogError(e);
        }
    }

    private static void StartServer()
    {
        Upnp.Open(Variables.Port);

        EventBasedNetListener listener = new();
        server = new(listener) { AutoRecycle = true };
        server.Start(Variables.Port);

        ServerLog.LogDebug("Listening for messages");

        listener.ConnectionRequestEvent += request => {
            if (server.ConnectedPeersCount < Variables.MaxConnections)
                request.AcceptIfKey(Variables.ConnectionKey);
            else
                request.Reject();
        };

        listener.PeerConnectedEvent += peer => {
            DateTime now = DateTime.UtcNow;
            ServerLog.LogDebug($"Connection: {peer.EndPoint} at {now:HH:mm:ss}.{now.Millisecond:D3}");

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

        ClientLog.LogDebug("Connecting to server");

        listener.PeerConnectedEvent += p => {
            ClientLog.LogDebug("Connected");
        };
        listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) => {
            DateTime now = DateTime.UtcNow;
            ClientLog.LogDebug($"Received \"{dataReader.GetString(100)}\" at {now:HH:mm:ss}.{now.Millisecond:D3}");
        };
    }
}
