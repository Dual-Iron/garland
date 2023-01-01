using BepInEx;
using BepInEx.Logging;
using Common;
using LiteNetLib;
using System;

namespace Client;

public enum ConnectionState
{
    Disconnected, Connecting, Connected, Error
}

[BepInPlugin("org.ozql.garland", "Garland", "0.1.0")]
sealed partial class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; } = BepInEx.Logging.Logger.CreateLogSource("Client");
    public static NetManager Client { get; private set; } = InitClient();
    public static ConnectionState ClientState { get; private set; }
    public static EnterSession? startPacket;

    private static NetManager InitClient()
    {
        EventBasedNetListener listener = new();
        listener.NetworkReceiveEvent += ReceivePacket;
        listener.PeerConnectedEvent += p => {
            ClientState = ConnectionState.Connected;
            Log.LogDebug("Connected");
        };
        listener.PeerDisconnectedEvent += (peer, info) => {
            ClientState = ConnectionState.Disconnected;
            Log.LogDebug($"Disconnected ({info.Reason})");
        };
        return new(listener);
    }

    private static void ReceivePacket(NetPeer peer, NetPacketReader data, DeliveryMethod deliveryMethod)
    {
        var type = (PacketKind)data.GetUShort();

        switch (type) {
            case PacketKind.EnterSession:
                EnterSession.Queue.Enqueue(data.Read<EnterSession>());
                break;

            default: Log.LogError($"Unknown packet type: 0x{type:X}"); break;
        }
    }

    public static void StopClient()
    {
        startPacket = null;
        ClientState = ConnectionState.Disconnected;
        Client.Stop(sendDisconnectMessages: true);
    }

    public static void StartConnecting(string address, int port)
    {
        StopClient();

        ClientState = ConnectionState.Connecting;
        Client.Start();
        Client.Connect(address, port, Variables.ConnectionKey);

        Log.LogDebug($"Connecting to {address}:{port}");
    }

    public void OnEnable()
    {
        try {
            MenuHooks();
            SessionHooks();

            On.RainWorld.Update += RainWorld_Update;
        }
        catch (Exception e) {
            Log.LogError(e);
        }
    }

    public void OnApplicationQuit()
    {
        StopClient();
    }

    private static void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    {
        orig(self);

        try {
            Client.PollEvents();
        }
        catch (Exception e) {
            Log.LogError(e);
        }
    }
}
