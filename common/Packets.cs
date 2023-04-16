using LiteNetLib;
using LiteNetLib.Utils;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;

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

    public static Player.InputPackage ToPackage(this Input input)
    {
        Player.InputPackage package = new(false, Options.ControlSetup.Preset.KeyboardSinglePlayer, 0, 0, input.Jump, input.Throw, input.Pickup, input.Map, false) {
            analogueDir = input.Dir
        };
        if (input.Dir.x > +0.5f) package.x = +1;
        if (input.Dir.x < -0.5f) package.x = -1;
        if (input.Dir.y > +0.5f) package.y = +1;
        if (input.Dir.y < -0.5f) { package.y = -1; package.downDiagonal = package.x; }
        return package;
    }

    public static Input ToPacket(this Player.InputPackage input)
    {
        Vector2 dir = input.analogueDir != default ? input.analogueDir : new Vector2(input.x, input.y).normalized;

        return new(dir, Input.ToBitmask(input.jmp, input.thrw, input.pckp, input.mp, input.mp));
    }

    #region Custom Get/Put methods for LiteNetLib
    public static Vector2 GetVec(this NetDataReader reader) => new(x: reader.GetFloat(), y: reader.GetFloat());
    public static Vector2[] GetVecArray(this NetDataReader reader)
    {
        Vector2[] ret = new Vector2[reader.GetUShort()];
        for (int i = 0; i < ret.Length; i++) {
            ret[i] = reader.GetVec();
        }
        return ret;
    }

    public static IntVector2 GetIVec(this NetDataReader reader) => new(p1: reader.GetInt(), p2: reader.GetInt());
    public static IntVector2[] GetIVecArray(this NetDataReader reader)
    {
        IntVector2[] ret = new IntVector2[reader.GetUShort()];
        for (int i = 0; i < ret.Length; i++) {
            ret[i] = reader.GetIVec();
        }
        return ret;
    }

    public static void Put(this NetDataWriter writer, Vector2 vec)
    {
        writer.Put(vec.x);
        writer.Put(vec.y);
    }
    public static void Put(this NetDataWriter writer, Vector2[] vec)
    {
        writer.Put((ushort)vec.Length);
        for (int i = 0; i < vec.Length; i++) {
            writer.Put(vec[i]);
        }
    }

    public static void Put(this NetDataWriter writer, IntVector2 vec)
    {
        writer.Put(vec.x);
        writer.Put(vec.y);
    }
    public static void Put(this NetDataWriter writer, IntVector2[] vec)
    {
        writer.Put((ushort)vec.Length);
        for (int i = 0; i < vec.Length; i++) {
            writer.Put(vec[i]);
        }
    }
    #endregion
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

    public IEnumerable<T> Drain()
    {
        while (awaiting.Count > 0) {
            yield return awaiting.Dequeue().Packet;
        }
    }
}

public interface IPacket : INetSerializable
{
    PacketKind GetKind();
}
