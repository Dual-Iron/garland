using BepInEx.Logging;
using Common;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using UnityEngine;

namespace Server;

partial class Main
{
    private static Main? instance;
    public static Main Instance => instance ??= new();
    public static ManualLogSource Log { get; } = BepInEx.Logging.Logger.CreateLogSource("Server");

    public readonly NetManager server;

    public Main()
    {
        // Parse command-line args
        int port = Utils.DefaultPort;

        foreach (string arg in Environment.GetCommandLineArgs()) {
            if (arg.StartsWith("-port=") && ushort.TryParse(arg.Substring("-port=".Length), out ushort newPort)) {
                port = newPort;
            }
        }

        if (port != Utils.DefaultPort) {
            Log.LogDebug($"Using non-standard port: {port}");
        }

        // Port forwarding
        Upnp.Open(port);

        // Start server
        EventBasedNetListener listener = new();

        server = new(listener) { AutoRecycle = true };

        listener.NetworkReceiveEvent += (peer, data, method) => Packets.QueuePacket(peer, data, Log);
        listener.ConnectionRequestEvent += request => {
            if (server.ConnectedPeersCount < Utils.MaxConnections)
                request.AcceptIfKey(Utils.ConnectionKey);
            else
                request.Reject();
        };
        listener.PeerConnectedEvent += peer => {
            DateTime now = DateTime.UtcNow;
            Log.LogDebug($"Connected to {peer.EndPoint} at {now:HH:mm:ss}.{now.Millisecond:D3}");

            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.session is ServerSession session) {
                string name = peer.EndPoint.ToString();

                if (session.AnyPeerConnected(name)) {
                    peer.Disconnect(NetDataWriter.FromString("Another client has already joined with that login."));
                    return;
                }

                var player = session.Join(peer, name, "1234");
                if (player.realizedObject == null) {
                    player.Room.AddEntity(player);
                    player.RealizeInRoom();
                }

                EnterSession packet = new((ushort)game.world.rainCycle.rainbowSeed, player.ID(), player.Room.name, ServerConfig.SlugcatWorld);

                peer.Send(packet, DeliveryMethod.ReliableOrdered);

                CatchUp(peer, game);
            }
        };
        listener.PeerDisconnectedEvent += (peer, info) => {
            Log.LogDebug($"Disconnected from {peer.EndPoint}: {info.Reason}");

            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game && game.session is ServerSession session) {
                session.Leave(peer);
            }
        };

        server.Start(port);

        Log.LogDebug("Ready for client connections");
    }

    public void Hook()
    {
        ObjectHooks();
        SessionHooks();
        GameHooks();

        On.RainWorld.Start += RainWorld_Start;
        On.RainWorld.Update += RainWorld_Update;

        // Small optimization
        On.SoundLoader.ShouldSoundPlay += delegate { return false; };
        // TODO see if this works...? On.RainWorldGame.GrafUpdate += delegate { };

        ManualLogSource unityLog = BepInEx.Logging.Logger.CreateLogSource("Unity");
        Application.logMessageReceived += (message, stackTrace, type) => {
            switch (type) {
                case LogType.Error:
                case LogType.Assert:
                    unityLog.LogError(message);
                    break;
                case LogType.Exception:
                    unityLog.LogError(message + $"\nStack trace:\n{stackTrace}");
                    break;
                case LogType.Warning:
                    unityLog.LogWarning(message);
                    break;
                default:
                    unityLog.LogInfo(message);
                    break;
            }
        };
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
