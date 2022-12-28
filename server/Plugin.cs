using BepInEx;
using BepInEx.Logging;
using Common;
using LiteNetLib;
using LiteNetLib.Utils;
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

    public void OnEnable()
    {
        GameHooks();

        On.RainWorld.Update += RainWorld_Update;

        // Small optimizations
        On.Music.MusicPlayer.Update += delegate { };
        On.SoundLoader.ShouldSoundPlay += delegate { return false; };
    }

    static void ParseArgs(out int port, out int? pid)
    {
        port = Variables.Port;
        pid = null;

        foreach (string arg in Environment.GetCommandLineArgs()) {
            if (arg.StartsWith("-port=") && int.TryParse(arg.Substring("-port=".Length), out int newPort) && newPort >= 0) {
                port = newPort;
            }
            else if (arg.StartsWith("-pid=") && int.TryParse(arg.Substring("-pid=".Length), out int newPid) && newPid >= 0) {
                pid = newPid;
            }
        }
    }

    private static NetManager StartServer()
    {
        // Parse command-line args
        ParseArgs(out int port, out int? pid);

        if (port != Variables.Port) {
            Log.LogDebug($"Using non-standard port: {port}");
        }

        if (pid != null) {
            Log.LogDebug($"Started from client process: {pid}");
            Plugin.pid = pid;
        }

        // Port forwarding
        Upnp.Open(port);

        // Start server
        EventBasedNetListener listener = new();
        NetManager server = new(listener) { AutoRecycle = true };
        server.Start(port);

        Log.LogDebug("Ready for client connections");

        listener.ConnectionRequestEvent += request => {
            if (server.ConnectedPeersCount < Variables.MaxConnections)
                request.AcceptIfKey(Variables.ConnectionKey);
            else
                request.Reject();
        };

        listener.PeerConnectedEvent += peer => {
            DateTime now = DateTime.UtcNow;
            Log.LogDebug($"Connected to {peer.EndPoint.Address} at {now:HH:mm:ss}.{now.Millisecond:D3}");

            NetDataWriter writer = new();
            writer.Put("Hello client!");
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        };

        listener.PeerDisconnectedEvent += (peer, info) => {
            Log.LogDebug($"Disconnected from {peer.EndPoint.Address}: {info.Reason}");
        };

        return server;
    }

    private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
    {
        // Don't play sounds from the console.
        AudioListener.pause = true;

        try {
            orig(self);
        }
        catch (Exception e) {
            Log.LogError($"Exception bubbled up to RainWorld.Update(). {e}");
        }

        try {
            // If opened by Rain World client, check if the client has exited every frame.
            if (pid.HasValue && Process.GetProcessById(pid.Value).HasExited) {
                Log.LogInfo("Client process closed. Stopping server.");

                server.Stop(sendDisconnectMessages: true);

                Application.Quit();
            }
            // Poll each update.
            else {
                server.PollEvents();
            }
        }
        catch (Exception e) {
            Log.LogError(e);
        }
    }
}
