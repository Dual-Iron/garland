using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Common;

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
    public T? Dequeue()
    {
        if (awaiting.Count == 0) return default;
        return awaiting.Dequeue();
    }

    /// <summary>
    /// Drains the queue empty. Useful for ignoring all but the latest packet.
    /// </summary>
    /// <returns>The item that was most recently added.</returns>
    public T? Drain()
    {
        if (awaiting.Count == 0) return default;
        while (awaiting.Count > 1) {
            awaiting.Dequeue();
        }
        return awaiting.Dequeue();
    }
}

public interface IPacket : INetSerializable
{
    PacketKind Kind { get; }
}
