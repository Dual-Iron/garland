using BepInEx.Logging;
using Common;
using LiteNetLib;
using System;

namespace Client;

partial class Main
{
    private static Main? instance;
    public static Main Instance => instance ??= new();
    public static ManualLogSource Log { get; } = Logger.CreateLogSource("Client");

    public NetManager Client { get; }
    public ConnectionState ClientState { get; private set; }
    public EnterSession? startPacket;

    public Main()
    {
        EventBasedNetListener listener = new();
        listener.NetworkReceiveEvent += (peer, data, method) => Packets.QueuePacket(data, Log);
        listener.PeerConnectedEvent += p => {
            ClientState = ConnectionState.Connected;
            Log.LogDebug("Connected");
        };
        listener.PeerDisconnectedEvent += (peer, info) => {
            ClientState = ConnectionState.Disconnected;
            Log.LogDebug($"Disconnected ({info.Reason})");
        };
        Client = new(listener);
    }

    public void StopClient()
    {
        startPacket = null;
        ClientState = ConnectionState.Disconnected;
        Client.Stop(sendDisconnectMessages: true);
    }

    public void StartConnecting(string address, int port)
    {
        StopClient();

        ClientState = ConnectionState.Connecting;
        Client.Start();
        Client.Connect(address, port, Utils.ConnectionKey);

        Log.LogDebug($"Connecting to {address}:{port}");
    }

    public void Hook()
    {
        WorldHooks();
        GameHooks();
        MenuHooks();
        SessionHooks();

        On.RainWorld.Update += RainWorld_Update;
    }

    private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    {
        try {
            Client.PollEvents();
        }
        catch (Exception e) {
            Log.LogError($"Exception in client logic. {e}");
        }

        try {
            orig(self);
        }
        catch (Exception e) {
            Log.LogError($"Exception in update logic. {e}");
        }
    }
}
