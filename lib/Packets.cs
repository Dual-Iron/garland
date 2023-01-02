using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;

namespace Common;

public static partial class Packets
{
    private static readonly NetDataWriter writer = new();

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

public sealed class PacketQueue<T> where T : struct
{
    readonly Queue<T> awaiting = new(16);

    /// <summary>
    /// Enqueues one item.
    /// </summary>
    /// <param name="value">The item to add to the queue.</param>
    public void Enqueue(T value) => awaiting.Enqueue(value);

    /// <summary>
    /// Dequeues one item.
    /// </summary>
    /// <returns>The item that was least recently added.</returns>
    public bool Dequeue(out T packet)
    {
        if (awaiting.Count == 0) {
            packet = default;
            return false;
        }
        packet = awaiting.Dequeue();
        return true;
    }

    /// <summary>
    /// Drains the queue empty. Useful for ignoring all but the latest packet.
    /// </summary>
    /// <returns>The item that was most recently added.</returns>
    public bool Latest(out T packet)
    {
        if (awaiting.Count == 0) {
            packet = default;
            return false;
        }
        while (awaiting.Count > 1) awaiting.Dequeue();
        packet = awaiting.Dequeue();
        return true;
    }
}

public interface IPacket : INetSerializable
{
    PacketKind GetKind();
}
