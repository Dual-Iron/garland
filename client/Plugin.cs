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

    private static RainWorld? rw;
    private static readonly EventBasedNetListener listener = new();

    public static NetManager Client { get; private set; } = new(listener);
    public static ConnectionState ClientState { get; private set; }

    public void OnEnable()
    {
        MenuHooks();
        SessionHooks();

        On.RainWorld.Start += RainWorld_Start;
        On.RainWorld.Update += RainWorld_Update;
    }

    private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
    {
        orig(self);
        rw = self;
    }

    private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    {
        orig(self);

        try {
            Client.PollEvents();
        }
        catch (Exception e) {
            Logger.LogError(e);
        }
    }

    public static void StopClient()
    {
        ClientState = ConnectionState.Disconnected;
        Client.Stop(sendDisconnectMessages: true);
    }

    public static void StartConnecting(string address, int port)
    {
        ClientState = ConnectionState.Connecting;

        Client.Stop(sendDisconnectMessages: true);
        Client.Start();

        Client.Connect(address, port, Variables.ConnectionKey);

        Log.LogDebug($"Connecting to {address}:{port}");
        listener.NetworkReceiveEvent += ProcessPacket;
        listener.PeerConnectedEvent += p => {
            ClientState = ConnectionState.Connected;
            Log.LogDebug("Connected");
        };
        listener.PeerDisconnectedEvent += (peer, info) => {
            ClientState = ConnectionState.Disconnected;
            Log.LogDebug($"Disconnected. {info.SocketErrorCode}: {info.Reason}");
        };
    }

    private static void ProcessPacket(NetPeer fromPeer, NetPacketReader dataReader, DeliveryMethod deliveryMethod)
    {
        ushort type = dataReader.GetUShort();

        if (type == 1) {
            ushort version = dataReader.GetUShort();
            string startRoom = dataReader.GetString();

            StartRoom = startRoom;
        }
    }

    // TODO use or delete this
    //private static void StartAndJoin(int port)
    //{
    //    ClientState = ConnectionState.StartingInternalServer;

    //    var startInfo = new ProcessStartInfo {
    //        WorkingDirectory = Variables.ServerPath(),
    //        FileName = Variables.ServerPath("RainWorld.exe"),
    //        Arguments = $"-batchmode -port={port} -pid={Process.GetCurrentProcess().Id}",
    //        UseShellExecute = false,
    //        CreateNoWindow = true,
    //        RedirectStandardOutput = true,
    //    };

    //    startInfo.EnvironmentVariables.Remove("DOORSTOP_INITIALIZED");
    //    startInfo.EnvironmentVariables.Remove("DOORSTOP_DISABLE");

    //    Process.Start(startInfo).Dispose();
    //}
}
