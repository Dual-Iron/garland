using LiteNetLib;
using LiteNetLib.Utils;
using RWCustom;
using System;
using UnityEngine;

namespace Common;

public enum PacketKind : ushort
{
    /// <summary>Sent to the server any time the client's input changes.</summary>
    Input = 0x100,
    /// <summary>Sent to clients joining a game session.</summary>
    EnterSession = 0x200,
    /// <summary>Sent every 15 seconds, after `GlobalRain.rainDirectionGetTo` changes, and after a client joins. Flood speed is a constant 0.2.</summary>
    SyncRain = 0x201,
    /// <summary>Sent every two seconds, after `DeathRain.deathRainMode` changes, and after a client joins. Only sent after death rain begins.</summary>
    SyncDeathRain = 0x202,
    /// <summary>Sent after each time AntiGravity toggles on or off. Progress is set to 0 each time the packet is received.</summary>
    SyncAntiGrav = 0x203,
    /// <summary>Tells a client to realize a room if it hasn't already.</summary>
    RealizeRoom = 0x204,
    /// <summary>Tells a client to abtractize a room if it hasn't already. TODO (low-priority)</summary>
    AbstractizeRoom = 0x205,
    /// <summary>Tells a client to destroy an object if it exists. TODO</summary>
    DestroyObject = 0x206,
    /// <summary>Tells a client that a creature is inside a shortcut. TODO (high-priority)</summary>
    SyncShortcut = 0x207,
    /// <summary>Introduces a player to the client. TODO (next)</summary>
    IntroPlayer = 0x210,
    /// <summary>Updates a player for a client.</summary>
    UpdatePlayer = 0x211,

}

public static partial class Packets
{
    public static void QueuePacket(NetPeer sender, NetPacketReader data, Action<string> error)
    {
        try {
            ushort type = data.GetUShort();

            switch (type) {
                case 0x100: Input.Queue.Enqueue(sender, data.Read<Input>()); break;
                case 0x200: EnterSession.Queue.Enqueue(sender, data.Read<EnterSession>()); break;
                case 0x201: SyncRain.Queue.Enqueue(sender, data.Read<SyncRain>()); break;
                case 0x202: SyncDeathRain.Queue.Enqueue(sender, data.Read<SyncDeathRain>()); break;
                case 0x203: SyncAntiGrav.Queue.Enqueue(sender, data.Read<SyncAntiGrav>()); break;
                case 0x204: RealizeRoom.Queue.Enqueue(sender, data.Read<RealizeRoom>()); break;
                case 0x205: AbstractizeRoom.Queue.Enqueue(sender, data.Read<AbstractizeRoom>()); break;
                case 0x206: DestroyObject.Queue.Enqueue(sender, data.Read<DestroyObject>()); break;
                case 0x207: SyncShortcut.Queue.Enqueue(sender, data.Read<SyncShortcut>()); break;
                case 0x210: IntroPlayer.Queue.Enqueue(sender, data.Read<IntroPlayer>()); break;
                case 0x211: UpdatePlayer.Queue.Enqueue(sender, data.Read<UpdatePlayer>()); break;

                default: error($"Invalid packet type: 0x{type:X}"); break;
            }

            if (data.AvailableBytes > 0) {
                error($"Packet is {data.AvailableBytes} bytes too large");
            }
        }
        catch (ArgumentException) {
            error("Packet is too small");
        }
    }
}

/// <summary>Sent to the server any time the client's input changes.</summary>
public record struct Input(Vector2 Dir, byte Bitmask) : IPacket
{
    public static PacketQueue<Input> Queue { get; } = new();

    public static bool Latest(out Input packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.Input;

    public void Deserialize(NetDataReader reader)
    {
        Dir = reader.GetVec();
        Bitmask = reader.GetByte();

    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Dir);
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

/// <summary>Sent to clients joining a game session.</summary>
public record struct EnterSession(byte SlugcatWorld, ushort RainbowSeed, int ClientPid, string StartingRoom) : IPacket
{
    public static PacketQueue<EnterSession> Queue { get; } = new();

    public static bool Latest(out EnterSession packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.EnterSession;

    public void Deserialize(NetDataReader reader)
    {
        SlugcatWorld = reader.GetByte();
        RainbowSeed = reader.GetUShort();
        ClientPid = reader.GetInt();
        StartingRoom = reader.GetString();

    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(SlugcatWorld);
        writer.Put(RainbowSeed);
        writer.Put(ClientPid);
        writer.Put(StartingRoom);

    }
}

/// <summary>Sent every 15 seconds, after `GlobalRain.rainDirectionGetTo` changes, and after a client joins. Flood speed is a constant 0.2.</summary>
public record struct SyncRain(ushort RainTimer, ushort RainTimerMax, float RainDirection, float RainDirectionGetTo) : IPacket
{
    public static PacketQueue<SyncRain> Queue { get; } = new();

    public static bool Latest(out SyncRain packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.SyncRain;

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

/// <summary>Sent every two seconds, after `DeathRain.deathRainMode` changes, and after a client joins. Only sent after death rain begins.</summary>
public record struct SyncDeathRain(byte DeathRainMode, float TimeInThisMode, float Progression, float CalmBeforeSunlight) : IPacket
{
    public static PacketQueue<SyncDeathRain> Queue { get; } = new();

    public static bool Latest(out SyncDeathRain packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.SyncDeathRain;

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

    public static bool Latest(out SyncAntiGrav packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.SyncAntiGrav;

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

/// <summary>Tells a client to realize a room if it hasn't already.</summary>
public record struct RealizeRoom(int Index) : IPacket
{
    public static PacketQueue<RealizeRoom> Queue { get; } = new();

    public static bool Latest(out RealizeRoom packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.RealizeRoom;

    public void Deserialize(NetDataReader reader)
    {
        Index = reader.GetInt();

    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Index);

    }
}

/// <summary>Tells a client to abtractize a room if it hasn't already. TODO (low-priority)</summary>
public record struct AbstractizeRoom(int Index) : IPacket
{
    public static PacketQueue<AbstractizeRoom> Queue { get; } = new();

    public static bool Latest(out AbstractizeRoom packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.AbstractizeRoom;

    public void Deserialize(NetDataReader reader)
    {
        Index = reader.GetInt();

    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Index);

    }
}

/// <summary>Tells a client to destroy an object if it exists. TODO</summary>
public record struct DestroyObject(int ID) : IPacket
{
    public static PacketQueue<DestroyObject> Queue { get; } = new();

    public static bool Latest(out DestroyObject packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.DestroyObject;

    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();

    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);

    }
}

/// <summary>Tells a client that a creature is inside a shortcut. TODO (high-priority)</summary>
public record struct SyncShortcut(int CreatureID, int RoomID, int EntranceNode, int Wait, IntVector2[] Positions) : IPacket
{
    public static PacketQueue<SyncShortcut> Queue { get; } = new();

    public static bool Latest(out SyncShortcut packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.SyncShortcut;

    public void Deserialize(NetDataReader reader)
    {
        CreatureID = reader.GetInt();
        RoomID = reader.GetInt();
        EntranceNode = reader.GetInt();
        Wait = reader.GetInt();
        Positions = reader.GetIVecArray();

    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CreatureID);
        writer.Put(RoomID);
        writer.Put(EntranceNode);
        writer.Put(Wait);
        writer.Put(Positions);

    }
}

/// <summary>Introduces a player to the client. TODO (next)</summary>
public record struct IntroPlayer(int ID, byte SkinR, byte SkinG, byte SkinB, float RunSpeed, float PoleClimbSpeed, float CorridorClimbSpeed, float BodyWeight, float Lungs, float Loudness, float VisBonus, float Stealth, byte ThrowingSkill, bool Ill) : IPacket
{
    public static PacketQueue<IntroPlayer> Queue { get; } = new();

    public static bool Latest(out IntroPlayer packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.IntroPlayer;

    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();
        SkinR = reader.GetByte();
        SkinG = reader.GetByte();
        SkinB = reader.GetByte();
        RunSpeed = reader.GetFloat();
        PoleClimbSpeed = reader.GetFloat();
        CorridorClimbSpeed = reader.GetFloat();
        BodyWeight = reader.GetFloat();
        Lungs = reader.GetFloat();
        Loudness = reader.GetFloat();
        VisBonus = reader.GetFloat();
        Stealth = reader.GetFloat();
        ThrowingSkill = reader.GetByte();
        Ill = reader.GetBool();

    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);
        writer.Put(SkinR);
        writer.Put(SkinG);
        writer.Put(SkinB);
        writer.Put(RunSpeed);
        writer.Put(PoleClimbSpeed);
        writer.Put(CorridorClimbSpeed);
        writer.Put(BodyWeight);
        writer.Put(Lungs);
        writer.Put(Loudness);
        writer.Put(VisBonus);
        writer.Put(Stealth);
        writer.Put(ThrowingSkill);
        writer.Put(Ill);

    }
}

/// <summary>Updates a player for a client.</summary>
public record struct UpdatePlayer(int ID, int Room, Vector2 HeadPos, Vector2 HeadVel, Vector2 ButtPos, Vector2 ButtVel, Vector2 InputDir, byte InputBitmask) : IPacket
{
    public static PacketQueue<UpdatePlayer> Queue { get; } = new();

    public static bool Latest(out UpdatePlayer packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.UpdatePlayer;

    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();
        Room = reader.GetInt();
        HeadPos = reader.GetVec();
        HeadVel = reader.GetVec();
        ButtPos = reader.GetVec();
        ButtVel = reader.GetVec();
        InputDir = reader.GetVec();
        InputBitmask = reader.GetByte();

    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);
        writer.Put(Room);
        writer.Put(HeadPos);
        writer.Put(HeadVel);
        writer.Put(ButtPos);
        writer.Put(ButtVel);
        writer.Put(InputDir);
        writer.Put(InputBitmask);

    }

    public static byte ToInputBitmask(bool Jump, bool Throw, bool Pickup, bool Point)
    {
        return (byte)((Jump ? 0x1 : 0) | (Throw ? 0x2 : 0) | (Pickup ? 0x4 : 0) | (Point ? 0x8 : 0));
    }

    public bool Jump => (InputBitmask & 0x1) != 0;
    public bool Throw => (InputBitmask & 0x2) != 0;
    public bool Pickup => (InputBitmask & 0x4) != 0;
    public bool Point => (InputBitmask & 0x8) != 0;

}

