using LiteNetLib.Utils;

namespace Common;

public enum PacketKind : ushort
{
    /// <summary>Sent to the server any time the client's input changes.</summary>
    Input = 0x100,
    /// <summary>Sent to a client after they join the game and begin a RainWorldGame instance.</summary>
    EnterSession = 0x200,
    /// <summary>Sent every two seconds, after `GlobalRain.rainDirectionGetTo` changes, and to newly-connected clients. Flood speed is a constant 0.2.</summary>
    SyncRain = 0x201,
    /// <summary>Sent every two seconds, after `DeathRain.deathRainMode` changes, and to newly-connected clients. Only sent after death rain begins.</summary>
    SyncDeathRain = 0x202,
    /// <summary>Sent after each time AntiGravity toggles on or off. Progress is set to 0 each time the packet is received.</summary>
    SyncAntiGrav = 0x203,
}

/// <summary>Sent to the server any time the client's input changes.</summary>
public record struct Input(float X, float Y, byte Bitmask) : IPacket
{
    public static PacketQueue<Input> Queue { get; } = new();

    public PacketKind Kind => PacketKind.Input;

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

    public static byte ToBitmask(bool Jump, bool Throw, bool Pickup, bool Point)
    {
        return (byte)((Jump ? 0x1 : 0) | (Throw ? 0x2 : 0) | (Pickup ? 0x4 : 0) | (Point ? 0x8 : 0));
    }

    public bool Jump => (Bitmask & 0x1) != 0;
    public bool Throw => (Bitmask & 0x2) != 0;
    public bool Pickup => (Bitmask & 0x4) != 0;
    public bool Point => (Bitmask & 0x8) != 0;
}

/// <summary>Sent to a client after they join the game and begin a RainWorldGame instance.</summary>
public record struct EnterSession(byte SlugcatWorldNumber, ushort RainbowSeed, string StartingRoom) : IPacket
{
    public static PacketQueue<EnterSession> Queue { get; } = new();

    public PacketKind Kind => PacketKind.EnterSession;

    public void Deserialize(NetDataReader reader)
    {
        SlugcatWorldNumber = reader.GetByte();
        RainbowSeed = reader.GetUShort();
        StartingRoom = reader.GetString();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(SlugcatWorldNumber);
        writer.Put(RainbowSeed);
        writer.Put(StartingRoom);
    }
}

/// <summary>Sent every two seconds, after `GlobalRain.rainDirectionGetTo` changes, and to newly-connected clients. Flood speed is a constant 0.2.</summary>
public record struct SyncRain(ushort RainTimer, ushort RainTimerMax, float RainDirection, float RainDirectionGetTo) : IPacket
{
    public static PacketQueue<SyncRain> Queue { get; } = new();

    public PacketKind Kind => PacketKind.SyncRain;

    public void Deserialize(NetDataReader reader)
    {
        RainTimer = reader.GetUShort();
        RainTimerMax = reader.GetUShort();
        RainDirection = reader.GetFloat();
        RainDirectionGetTo = reader.GetFloat();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RainTimer);
        writer.Put(RainTimerMax);
        writer.Put(RainDirection);
        writer.Put(RainDirectionGetTo);
    }
}

/// <summary>Sent every two seconds, after `DeathRain.deathRainMode` changes, and to newly-connected clients. Only sent after death rain begins.</summary>
public record struct SyncDeathRain(byte DeathRainMode, float TimeInThisMode, float Progression, float CalmBeforeSunlight) : IPacket
{
    public static PacketQueue<SyncDeathRain> Queue { get; } = new();

    public PacketKind Kind => PacketKind.SyncDeathRain;

    public void Deserialize(NetDataReader reader)
    {
        DeathRainMode = reader.GetByte();
        TimeInThisMode = reader.GetFloat();
        Progression = reader.GetFloat();
        CalmBeforeSunlight = reader.GetFloat();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(DeathRainMode);
        writer.Put(TimeInThisMode);
        writer.Put(Progression);
        writer.Put(CalmBeforeSunlight);
    }
}

/// <summary>Sent after each time AntiGravity toggles on or off. Progress is set to 0 each time the packet is received.</summary>
public record struct SyncAntiGrav(bool On, ushort Counter, float From, float To) : IPacket
{
    public static PacketQueue<SyncAntiGrav> Queue { get; } = new();

    public PacketKind Kind => PacketKind.SyncAntiGrav;

    public void Deserialize(NetDataReader reader)
    {
        On = reader.GetBool();
        Counter = reader.GetUShort();
        From = reader.GetFloat();
        To = reader.GetFloat();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(On);
        writer.Put(Counter);
        writer.Put(From);
        writer.Put(To);
    }
}

