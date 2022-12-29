using LiteNetLib.Utils;
using System.Collections.Generic;

namespace Common;

// TODO: modify EnterSession and implement SyncRain, SyncDeathRain, and SyncAntiGrav
public enum PacketType : ushort
{
    // ~~ Client -> Server ~~

    // f32 X
    // f32 Y
    // u8  Bitmask { Jump = 0x1, Throw = 0x2, Pickup = 0x4, Point = 0x8 }
    Input = 0x100,

    // ~~ Server -> Client ~~

    // u8  SlugcatWorldNumber
    // u16 RainbowSeed
    // str StartingRoom
    EnterSession = 0x200,

    // Sent every two seconds, after `GlobalRain.rainDirectionGetTo` changes, and to newly-connected clients. Flood speed is a constant 0.2.
    // u16 RainTimer
    // u16 RainTimerMax
    // f32 RainDirection
    // f32 RainDirectionGetTo
    SyncRain = 0x201,

    // After death rain begins, this is sent every two seconds, after `DeathRain.deathRainMode` changes, and to newly-connected clients.
    // u8  DeathRainMode
    // f32 TimeInThisMode
    // f32 Progression
    // f32 CalmBeforeSunlight
    SyncDeathRain = 0x202,

    // Sent after each time AntiGravity toggles on or off. Progress is set to 0 after.
    // bool On
    // u16  Counter
    // f32  From
    // f32  To
    SyncAntiGrav = 0x203,
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
    PacketType Type { get; }
}

// TODO this should be generated code, lol
public record struct Input(float X, float Y, byte Bitmask) : IPacket
{
    public static readonly PacketQueue<Input> Queue = new();

    public static byte ToBitmask(bool Jump, bool Throw, bool Pickup, bool Point)
    {
        return (byte)((Jump ? 0x1 : 0) | (Throw ? 0x2 : 0) | (Pickup ? 0x4 : 0) | (Point ? 0x8 : 0));
    }

    public PacketType Type => PacketType.Input;

    public void Deserialize(NetDataReader reader)
    {
        X = reader.GetFloat();
        Y = reader.GetFloat();
        Bitmask = reader.GetByte();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(X);
        writer.Put(Y);
        writer.Put(Bitmask);
    }

    public bool Jump => (Bitmask & 0x1) != 0;
    public bool Throw => (Bitmask & 0x2) != 0;
    public bool Pickup => (Bitmask & 0x4) != 0;
    public bool Point => (Bitmask & 0x8) != 0;
}

public record struct EnterSession(ushort RainTimer, ushort RainTimerMax, ushort RainbowSeed, string StartingRoom) : IPacket
{
    public static readonly PacketQueue<EnterSession> Queue = new();

    public PacketType Type => PacketType.EnterSession;

    public void Deserialize(NetDataReader reader)
    {
        RainTimer = reader.GetUShort();
        RainTimerMax = reader.GetUShort();
        RainbowSeed = reader.GetUShort();
        StartingRoom = reader.GetString();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RainTimer);
        writer.Put(RainTimerMax);
        writer.Put(RainbowSeed);
        writer.Put(StartingRoom);
    }
}
