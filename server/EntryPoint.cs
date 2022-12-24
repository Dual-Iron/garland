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

static void WriteLineColored(ConsoleColor color, string message)
{
    ConsoleColor precolor = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ForegroundColor = precolor;
}

while (true) {
    while (server.ReadMessage(out NetIncomingMessage message)) {
        switch (message.MessageType) {
            case NetIncomingMessageType.DebugMessage:
            case NetIncomingMessageType.VerboseDebugMessage:
                WriteLineColored(ConsoleColor.DarkGray, message.ReadString());
                break;

            case NetIncomingMessageType.WarningMessage:
                WriteLineColored(ConsoleColor.Yellow, message.ReadString());
                break;

            case NetIncomingMessageType.ErrorMessage:
                WriteLineColored(ConsoleColor.Red, message.ReadString());
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
