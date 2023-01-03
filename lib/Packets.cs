using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;

namespace Common;

public static partial class Packets
{
    private static readonly NetDataWriter writer = new();

    public static void QueuePacket(NetPeer sender, NetPacketReader data, BepInEx.Logging.ManualLogSource logger)
    {
        QueuePacket(sender, data, logger.LogWarning);
    }

    public static void Send<T>(this NetPeer peer, T value, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : IPacket
    {
        writer.Reset();
        writer.Put((ushort)value.GetKind());
        writer.Put(value);
        peer.Send(writer, deliveryMethod);
    }

    public static void Broadcast<T>(this NetManager server, T value, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : IPacket
    {
        writer.Reset();
        writer.Put((ushort)value.GetKind());
        writer.Put(value);
        server.SendToAll(writer, deliveryMethod);
    }

    public static T Read<T>(this NetDataReader reader) where T : struct, INetSerializable
    {
        T value = default;
        value.Deserialize(reader);
        return value;
    }
}

// TODO: add a limit to PacketQueue after which old packets are dropped
public sealed class PacketQueue<T> where T : struct
{
    record struct ReceivedPacket(NetPeer Sender, T Packet);

    readonly Queue<ReceivedPacket> awaiting = new(16);

    /// <summary>Enqueues one item.</summary>
    /// <param name="value">The item to add.</param>
    public void Enqueue(NetPeer sender, T value) => awaiting.Enqueue(new(sender, value));

    /// <summary>Dequeues one item.</summary>
    /// <returns>The item that was least recently added.</returns>
    public bool Dequeue(out NetPeer sender, out T packet)
    {
        if (awaiting.Count == 0) {
            sender = null!;
            packet = default;
            return false;
        }
        awaiting.Dequeue().Deconstruct(out sender, out packet);
        return true;
    }

    /// <summary>Drains the queue empty. Useful for ignoring all but the latest packet.</summary>
    /// <returns>The item that was most recently added.</returns>
    public bool Latest(out NetPeer sender, out T packet)
    {
        if (awaiting.Count == 0) {
            sender = default!;
            packet = default;
            return false;
        }
        while (awaiting.Count > 1) awaiting.Dequeue();
        awaiting.Dequeue().Deconstruct(out sender, out packet);
        return true;
    }
}

public interface IPacket : INetSerializable
{
    PacketKind GetKind();
}
