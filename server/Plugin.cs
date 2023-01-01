using BepInEx;
using BepInEx.Logging;
using Common;
using LiteNetLib;
using System;
using System.Diagnostics;
using System.Security.Permissions;
using UnityEngine;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Server;

[BepInPlugin("org.ozql.garland", "Garland", "0.1.0")]
sealed partial class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log { get; } = BepInEx.Logging.Logger.CreateLogSource("Server");
    public static NetManager server = StartServer();

    static int? pid;

    private static NetManager StartServer()
    {
        // Parse command-line args
        int port = Variables.DefaultPort;

        foreach (string arg in Environment.GetCommandLineArgs()) {
            if (arg.StartsWith("-port=") && ushort.TryParse(arg.Substring("-port=".Length), out ushort newPort)) {
                port = newPort;
            }
            else if (arg.StartsWith("-pid=") && int.TryParse(arg.Substring("-pid=".Length), out int newPid)) {
                pid = newPid;
            }
        }

        if (port != Variables.DefaultPort) {
            Log.LogDebug($"Using non-standard port: {port}");
        }

        if (pid != null) {
            Log.LogDebug($"Started from client process: {pid}");
        }

        // Port forwarding
        Upnp.Open(port);

        // Start server
        EventBasedNetListener listener = new();
        NetManager server = new(listener) { AutoRecycle = true };

        listener.NetworkReceiveEvent += (peer, data, method) => Packets.QueuePacket(data, Log);
        listener.ConnectionRequestEvent += request => {
            if (server.ConnectedPeersCount < Variables.MaxConnections)
                request.AcceptIfKey(Variables.ConnectionKey);
            else
                request.Reject();
        };
        listener.PeerConnectedEvent += peer => {
            DateTime now = DateTime.UtcNow;
            Log.LogDebug($"Connected to {peer.EndPoint.Address} at {now:HH:mm:ss}.{now.Millisecond:D3}");

            if (Utils.Rw.processManager.currentMainLoop is RainWorldGame game) {
                EnterSession packet = new(ServerConfig.SlugcatWorld, (ushort)game.world.rainCycle.rainbowSeed, ServerConfig.StartingRoom);

                peer.Send(packet, DeliveryMethod.ReliableOrdered);
            }
        };
        listener.PeerDisconnectedEvent += (peer, info) => {
            Log.LogDebug($"Disconnected from {peer.EndPoint.Address}: {info.Reason}");
        };

        server.Start(port);

        Log.LogDebug("Ready for client connections");

        return server;
    }

    public void OnEnable()
    {
        try {
            GameHooks();

            On.RainWorld.Start += RainWorld_Start;
            On.RainWorld.Update += RainWorld_Update;

            // Small optimization
            On.SoundLoader.ShouldSoundPlay += delegate { return false; };
        }
        catch (Exception e) {
            Log.LogFatal(e);
        }
    }

    private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
    {
        try {
            orig(self);
        }
        catch (Exception e) {
            Log.LogFatal(e);

            Application.Quit();
        }
    }

    private static void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    {
        // Don't play sounds from the console.
        AudioListener.pause = true;

        try {
            orig(self);
        }
        catch (Exception e) {
            Log.LogError($"Exception in update logic. {e}");
        }

        try {
            server.PollEvents();
        }
        catch (Exception e) {
            Log.LogError($"Exception in server logic. {e}");
        }

        // If opened by Rain World client, check if the client has exited every frame.
        if (pid.HasValue) {
            using var p = Process.GetProcessById(pid.Value);
            if (p.HasExited) {
                Log.LogInfo("Client process closed. Stopping server.");

                server.Stop(sendDisconnectMessages: true);

                Application.Quit();
            }
        }
    }
}
