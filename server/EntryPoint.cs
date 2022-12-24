using Common;
using Lidgren.Network;
using System;
using System.Net;

NetServer server = new(new NetPeerConfiguration("Garland!") {
    Port = Variables.Port,
    LocalAddress = IPAddress.IPv6Any,
    DualStack = true,
    EnableUPnP = true,
});

server.Start();

Console.WriteLine("Listening for messages.");

while (true) {
    while (server.ReadMessage(out NetIncomingMessage message)) {
        switch (message.MessageType) {
            case NetIncomingMessageType.DebugMessage:
            case NetIncomingMessageType.WarningMessage:
            case NetIncomingMessageType.VerboseDebugMessage:
                Console.WriteLine(message.ReadString());
                break;

            case NetIncomingMessageType.ErrorMessage:
                ConsoleColor color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(message.ReadString());
                Console.ForegroundColor = color;
                break;

            case NetIncomingMessageType.StatusChanged:
                NetConnectionStatus status = (NetConnectionStatus)message.ReadByte();

                string reason = message.ReadString();

                Console.WriteLine($"{NetUtility.ToHexString(message.SenderConnection.RemoteUniqueIdentifier)} {status}: {reason}");

                if (status == NetConnectionStatus.Connected) {
                    Console.WriteLine($"Hello {message.SenderEndPoint}! Remote hail: {message.SenderConnection.RemoteHailMessage.ReadString()}");
                }
                if (status == NetConnectionStatus.Disconnected) {
                    Console.WriteLine($"Bye {message.SenderEndPoint}.");
                }
                break;
        }
    }
    System.Threading.Thread.Sleep(1);
}
