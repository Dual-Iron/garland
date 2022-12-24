using BepInEx;
using BepInEx.Logging;
using Common;
using Lidgren.Network;
using System;

namespace Client;

[BepInPlugin("org.ozql.garland", "Garland", "0.1.0")]
sealed class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger { get; private set; }

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
            UpdateNetwork(self);
        }
        catch (Exception e) {
            Logger.LogError(e);
        }
    }

    static bool init = true;
    static NetClient client;

    private void UpdateNetwork(RainWorld self)
    {
        if (init) {
            init = false;
            client = new(new NetPeerConfiguration("Garland!"));

            client.Start();
            client.Connect("localhost", Variables.Port, client.CreateMessage("<3"));
        }

        while (client.ReadMessage(out NetIncomingMessage message)) {
            switch (message.MessageType) {
                case NetIncomingMessageType.DebugMessage:
                case NetIncomingMessageType.VerboseDebugMessage:
                    Logger.LogDebug(message.ReadString());
                    break;

                case NetIncomingMessageType.WarningMessage:
                    Logger.LogWarning(message.ReadString());
                    break;

                case NetIncomingMessageType.ErrorMessage:
                    Logger.LogError(message.ReadString());
                    break;

                case NetIncomingMessageType.StatusChanged:
                    NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                    string reason = message.ReadString();

                    if (status == NetConnectionStatus.Connected) {
                        Console.WriteLine($"Connected to server!! {status}: {reason}");
                    }
                    else {
                        Console.WriteLine($"Oops. {status}: {reason}");
                    }
                    break;
            }
        }
    }
}
