using Common;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Threading;

UpnpHelp.Upnp.Open(Variables.Port);

EventBasedNetListener listener = new();
NetManager server = new(listener) { AutoRecycle = true };
server.Start(Variables.Port);

Console.WriteLine("Listening for messages.");

listener.ConnectionRequestEvent += request => {
    if (server.ConnectedPeersCount < Variables.MaxConnections)
        request.AcceptIfKey(Variables.ConnectionKey);
    else
        request.Reject();
};

listener.PeerConnectedEvent += peer => {
    DateTime now = DateTime.UtcNow;
    Console.WriteLine($"Connection: {peer.EndPoint} at {now:HH:mm:ss}.{now.Millisecond:D3}");

    NetDataWriter writer = new();
    writer.Put("Hello client!");
    peer.Send(writer, DeliveryMethod.ReliableOrdered);
};

while (!Console.KeyAvailable) {
    server.PollEvents();
    Thread.Sleep(15);
}

server.Stop();
