using BepInEx.Logging;
using Common;
using LiteNetLib;
using System;
using UnityEngine;

namespace Server;

partial class Main
{
    private static Main? instance;
    public static Main Instance => instance ??= new();
    public static ManualLogSource Log { get; } = Logger.CreateLogSource("Server");

    public bool ClientJustJoined { get; private set; }
    public readonly NetManager server;

    public Main()
    {
        // Parse command-line args
        int port = Variables.DefaultPort;

        foreach (string arg in Environment.GetCommandLineArgs()) {
            if (arg.StartsWith("-port=") && ushort.TryParse(arg.Substring("-port=".Length), out ushort newPort)) {
                port = newPort;
            }
        }

        if (port != Variables.DefaultPort) {
            Log.LogDebug($"Using non-standard port: {port}");
        }

        // Port forwarding
        Upnp.Open(port);

        // Start server
        EventBasedNetListener listener = new();

        server = new(listener) { AutoRecycle = true };

        listener.NetworkReceiveEvent += (peer, data, method) => Packets.QueuePacket(data, Log);
        listener.ConnectionRequestEvent += request => {
            if (server.ConnectedPeersCount < Variables.MaxConnections)
                request.AcceptIfKey(Variables.ConnectionKey);
            else
                request.Reject();
        };
        listener.PeerConnectedEvent += peer => {
            ClientJustJoined = true;

            DateTime now = DateTime.UtcNow;
            Log.LogDebug($"Connected to {peer.EndPoint.Address} at {now:HH:mm:ss}.{now.Millisecond:D3}");

            if (Utils.Rw.processManager.currentMainLoop is RainWorldGame game && game.session is ServerSession session) {
                var player = session.Join(peer, peer.EndPoint.ToString());
                player.Room.AddEntity(player);
                player.RealizeInRoom();

                EnterSession packet = new(ServerConfig.SlugcatWorld, (ushort)game.world.rainCycle.rainbowSeed, player.ID.number, ServerConfig.StartingRoom);

                peer.Send(packet, DeliveryMethod.ReliableOrdered);
            }
        };
        listener.PeerDisconnectedEvent += (peer, info) => {
            Log.LogDebug($"Disconnected from {peer.EndPoint.Address}: {info.Reason}");
        };

        server.Start(port);

        Log.LogDebug("Ready for client connections");
    }

    public void Hook()
    {
        SessionHooks();
        GameHooks();

        On.RainWorld.Start += RainWorld_Start;
        On.RainWorld.Update += RainWorld_Update;

        // Small optimization
        On.SoundLoader.ShouldSoundPlay += delegate { return false; };
    }

    private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
    {
        try {
            orig(self);
        }
        catch (Exception e) {
            Log.LogFatal(e);

            Application.Quit();
        }
    }

    private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    {
        // Don't play sounds from the console.
        AudioListener.pause = true;

        ClientJustJoined = false;

        try {
            server.PollEvents();
        }
        catch (Exception e) {
            Log.LogError($"Exception in server logic. {e}");
        }

        try {
            orig(self);
        }
        catch (Exception e) {
            Log.LogError($"Exception in update logic. {e}");
        }
    }
}
