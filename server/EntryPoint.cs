using Common;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Threading;

EventBasedNetListener listener = new();
NetManager server = new(listener);
server.Start(Variables.Port);

Console.WriteLine("Listening for messages.");

listener.ConnectionRequestEvent += request => {
    if (server.ConnectedPeersCount < Variables.MaxConnections)
        request.AcceptIfKey(Variables.ConnectionKey);
    else
        request.Reject();
};

listener.PeerConnectedEvent += peer => {
    Console.WriteLine($"We got connection: {peer.EndPoint}"); // Show peer ip
    NetDataWriter writer = new();                 // Create writer class
    writer.Put("Hello client!");                                // Put some string
    peer.Send(writer, DeliveryMethod.ReliableOrdered);             // Send with reliability
};

while (!Console.KeyAvailable) {
    server.PollEvents();
    Thread.Sleep(15);
}

server.Stop();
