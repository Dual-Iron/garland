using LiteNetLib;
using LiteNetLib.Utils;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common;

public enum PacketKind : ushort
{
    /// <summary>Sent to the server any time the client's input changes.</summary>
    Input = 0x100,
    /// <summary>Sent to clients joining a game session.</summary>
    EnterSession = 0x200,
    /// <summary>Sent every 15 seconds, after `GlobalRain.rainDirectionGetTo` changes, and after a client joins.</summary>
    SyncRain = 0x201,
    /// <summary>Sent every two seconds, after `DeathRain.deathRainMode` changes, and after a client joins. Only sent after death rain begins.</summary>
    SyncDeathRain = 0x202,
    /// <summary>Sent after each time AntiGravity toggles on or off. Progress is set to 0 each time the packet is received.</summary>
    SyncAntiGrav = 0x203,
    /// <summary>Tells a client to realize a room if it hasn't already.</summary>
    RealizeRoom = 0x204,
    /// <summary>Tells a client to abtractize a room if it hasn't already. TODO (low-priority)</summary>
    AbstractizeRoom = 0x205,
    /// <summary>Tells a client to destroy an object if it exists.</summary>
    DestroyObject = 0x206,
    /// <summary>Kills a creature for a client.</summary>
    KillCreature = 0x207,
    /// <summary>Tells a client that a creature is inside a shortcut. TODO (high-priority)</summary>
    SyncShortcut = 0x208,
    /// <summary>Makes a creature grab an object.</summary>
    Grab = 0x209,
    /// <summary>Makes a creature release an object.</summary>
    Release = 0x20A,
    /// <summary>Introduces a player to a client.</summary>
    IntroPlayer = 0x250,
    /// <summary>Updates a player for a client.</summary>
    UpdatePlayer = 0x251,
    /// <summary>TODO!!</summary>
    IntroFly = 0x252,
    UpdateFly = 0x253,

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
                case 0x207: KillCreature.Queue.Enqueue(sender, data.Read<KillCreature>()); break;
                case 0x208: SyncShortcut.Queue.Enqueue(sender, data.Read<SyncShortcut>()); break;
                case 0x209: Grab.Queue.Enqueue(sender, data.Read<Grab>()); break;
                case 0x20A: Release.Queue.Enqueue(sender, data.Read<Release>()); break;
                case 0x250: IntroPlayer.Queue.Enqueue(sender, data.Read<IntroPlayer>()); break;
                case 0x251: UpdatePlayer.Queue.Enqueue(sender, data.Read<UpdatePlayer>()); break;
                case 0x252: IntroFly.Queue.Enqueue(sender, data.Read<IntroFly>()); break;
                case 0x253: UpdateFly.Queue.Enqueue(sender, data.Read<UpdateFly>()); break;

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
    public static IEnumerable<Input> All() => Queue.Drain();
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
    public static byte ToBitmask(bool Jump, bool Throw, bool Pickup, bool Point, bool Map)
    {
        return (byte)((Jump ? 0x1 : 0) | (Throw ? 0x2 : 0) | (Pickup ? 0x4 : 0) | (Point ? 0x8 : 0) | (Map ? 0x16 : 0));
    }
    public bool Jump => (Bitmask & 0x1) != 0;
    public bool Throw => (Bitmask & 0x2) != 0;
    public bool Pickup => (Bitmask & 0x4) != 0;
    public bool Point => (Bitmask & 0x8) != 0;
    public bool Map => (Bitmask & 0x16) != 0;

}
/// <summary>Sent to clients joining a game session.</summary>
public record struct EnterSession(ushort RainbowSeed, int ClientPid, string StartingRoom, string SlugcatWorld) : IPacket
{
    public static PacketQueue<EnterSession> Queue { get; } = new();
    public static bool Latest(out EnterSession packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<EnterSession> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.EnterSession;
    public void Deserialize(NetDataReader reader)
    {
        RainbowSeed = reader.GetUShort();
        ClientPid = reader.GetInt();
        StartingRoom = reader.GetString();
        SlugcatWorld = reader.GetString();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(RainbowSeed);
        writer.Put(ClientPid);
        writer.Put(StartingRoom);
        writer.Put(SlugcatWorld);

    }
}
/// <summary>Sent every 15 seconds, after `GlobalRain.rainDirectionGetTo` changes, and after a client joins.</summary>
public record struct SyncRain(int RainTimer, int RainTimerMax, float RainDirection, float RainDirectionGetTo) : IPacket
{
    public static PacketQueue<SyncRain> Queue { get; } = new();
    public static bool Latest(out SyncRain packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<SyncRain> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.SyncRain;
    public void Deserialize(NetDataReader reader)
    {
        RainTimer = reader.GetInt();
        RainTimerMax = reader.GetInt();
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
public record struct SyncDeathRain(float TimeInThisMode, float Progression, float CalmBeforeSunlight, float Flood, float FloodSpeed, string DeathRainMode) : IPacket
{
    public static PacketQueue<SyncDeathRain> Queue { get; } = new();
    public static bool Latest(out SyncDeathRain packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<SyncDeathRain> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.SyncDeathRain;
    public void Deserialize(NetDataReader reader)
    {
        TimeInThisMode = reader.GetFloat();
        Progression = reader.GetFloat();
        CalmBeforeSunlight = reader.GetFloat();
        Flood = reader.GetFloat();
        FloodSpeed = reader.GetFloat();
        DeathRainMode = reader.GetString();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(TimeInThisMode);
        writer.Put(Progression);
        writer.Put(CalmBeforeSunlight);
        writer.Put(Flood);
        writer.Put(FloodSpeed);
        writer.Put(DeathRainMode);

    }
}
/// <summary>Sent after each time AntiGravity toggles on or off. Progress is set to 0 each time the packet is received.</summary>
public record struct SyncAntiGrav(bool On, ushort Counter, float From, float To) : IPacket
{
    public static PacketQueue<SyncAntiGrav> Queue { get; } = new();
    public static bool Latest(out SyncAntiGrav packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<SyncAntiGrav> All() => Queue.Drain();
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
public record struct RealizeRoom(string Room) : IPacket
{
    public static PacketQueue<RealizeRoom> Queue { get; } = new();
    public static bool Latest(out RealizeRoom packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<RealizeRoom> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.RealizeRoom;
    public void Deserialize(NetDataReader reader)
    {
        Room = reader.GetString();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Room);

    }
}
/// <summary>Tells a client to abtractize a room if it hasn't already. TODO (low-priority)</summary>
public record struct AbstractizeRoom(string Room) : IPacket
{
    public static PacketQueue<AbstractizeRoom> Queue { get; } = new();
    public static bool Latest(out AbstractizeRoom packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<AbstractizeRoom> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.AbstractizeRoom;
    public void Deserialize(NetDataReader reader)
    {
        Room = reader.GetString();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Room);

    }
}
/// <summary>Tells a client to destroy an object if it exists.</summary>
public record struct DestroyObject(int ID) : IPacket
{
    public static PacketQueue<DestroyObject> Queue { get; } = new();
    public static bool Latest(out DestroyObject packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<DestroyObject> All() => Queue.Drain();
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
/// <summary>Kills a creature for a client.</summary>
public record struct KillCreature(int ID) : IPacket
{
    public static PacketQueue<KillCreature> Queue { get; } = new();
    public static bool Latest(out KillCreature packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<KillCreature> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.KillCreature;
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
public record struct SyncShortcut(int CreatureID, string Room, int EntranceNode, int Wait, IntVector2[] Positions) : IPacket
{
    public static PacketQueue<SyncShortcut> Queue { get; } = new();
    public static bool Latest(out SyncShortcut packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<SyncShortcut> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.SyncShortcut;
    public void Deserialize(NetDataReader reader)
    {
        CreatureID = reader.GetInt();
        Room = reader.GetString();
        EntranceNode = reader.GetInt();
        Wait = reader.GetInt();
        Positions = reader.GetIVecArray();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(CreatureID);
        writer.Put(Room);
        writer.Put(EntranceNode);
        writer.Put(Wait);
        writer.Put(Positions);

    }
}
/// <summary>Makes a creature grab an object.</summary>
public record struct Grab(int GrabbedID, int GrabberID, float Dominance, byte GraspUsed, byte GrabbedChunk, byte Bitmask) : IPacket
{
    public static PacketQueue<Grab> Queue { get; } = new();
    public static bool Latest(out Grab packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<Grab> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.Grab;
    public void Deserialize(NetDataReader reader)
    {
        GrabbedID = reader.GetInt();
        GrabberID = reader.GetInt();
        Dominance = reader.GetFloat();
        GraspUsed = reader.GetByte();
        GrabbedChunk = reader.GetByte();
        Bitmask = reader.GetByte();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(GrabbedID);
        writer.Put(GrabberID);
        writer.Put(Dominance);
        writer.Put(GraspUsed);
        writer.Put(GrabbedChunk);
        writer.Put(Bitmask);

    }
    public static byte ToBitmask(bool NonExclusive, bool ShareWithNonExclusive, bool OverrideEquallyDominant, bool Pacifying)
    {
        return (byte)((NonExclusive ? 0x1 : 0) | (ShareWithNonExclusive ? 0x2 : 0) | (OverrideEquallyDominant ? 0x4 : 0) | (Pacifying ? 0x8 : 0));
    }
    public bool NonExclusive => (Bitmask & 0x1) != 0;
    public bool ShareWithNonExclusive => (Bitmask & 0x2) != 0;
    public bool OverrideEquallyDominant => (Bitmask & 0x4) != 0;
    public bool Pacifying => (Bitmask & 0x8) != 0;

}
/// <summary>Makes a creature release an object.</summary>
public record struct Release(int GrabbedID, int GrabberID, byte GraspUsed) : IPacket
{
    public static PacketQueue<Release> Queue { get; } = new();
    public static bool Latest(out Release packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<Release> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.Release;
    public void Deserialize(NetDataReader reader)
    {
        GrabbedID = reader.GetInt();
        GrabberID = reader.GetInt();
        GraspUsed = reader.GetByte();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(GrabbedID);
        writer.Put(GrabberID);
        writer.Put(GraspUsed);

    }
}
/// <summary>Introduces a player to a client.</summary>
public record struct IntroPlayer(int ID, string Room, byte SkinR, byte SkinG, byte SkinB, byte EyeR, byte EyeG, byte EyeB, float Fat, float Speed, float Charm, byte FoodMax, byte FoodSleep, float RunSpeed, float PoleClimbSpeed, float CorridorClimbSpeed, float Weight, float VisBonus, float SneakStealth, float Loudness, float LungWeakness, byte Bitmask) : IPacket
{
    public static PacketQueue<IntroPlayer> Queue { get; } = new();
    public static bool Latest(out IntroPlayer packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<IntroPlayer> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.IntroPlayer;
    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();
        Room = reader.GetString();
        SkinR = reader.GetByte();
        SkinG = reader.GetByte();
        SkinB = reader.GetByte();
        EyeR = reader.GetByte();
        EyeG = reader.GetByte();
        EyeB = reader.GetByte();
        Fat = reader.GetFloat();
        Speed = reader.GetFloat();
        Charm = reader.GetFloat();
        FoodMax = reader.GetByte();
        FoodSleep = reader.GetByte();
        RunSpeed = reader.GetFloat();
        PoleClimbSpeed = reader.GetFloat();
        CorridorClimbSpeed = reader.GetFloat();
        Weight = reader.GetFloat();
        VisBonus = reader.GetFloat();
        SneakStealth = reader.GetFloat();
        Loudness = reader.GetFloat();
        LungWeakness = reader.GetFloat();
        Bitmask = reader.GetByte();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);
        writer.Put(Room);
        writer.Put(SkinR);
        writer.Put(SkinG);
        writer.Put(SkinB);
        writer.Put(EyeR);
        writer.Put(EyeG);
        writer.Put(EyeB);
        writer.Put(Fat);
        writer.Put(Speed);
        writer.Put(Charm);
        writer.Put(FoodMax);
        writer.Put(FoodSleep);
        writer.Put(RunSpeed);
        writer.Put(PoleClimbSpeed);
        writer.Put(CorridorClimbSpeed);
        writer.Put(Weight);
        writer.Put(VisBonus);
        writer.Put(SneakStealth);
        writer.Put(Loudness);
        writer.Put(LungWeakness);
        writer.Put(Bitmask);

    }
    public static byte ToBitmask(bool Ill, bool EatsMeat, bool Glows, bool HasMark)
    {
        return (byte)((Ill ? 0x1 : 0) | (EatsMeat ? 0x2 : 0) | (Glows ? 0x4 : 0) | (HasMark ? 0x8 : 0));
    }
    public bool Ill => (Bitmask & 0x1) != 0;
    public bool EatsMeat => (Bitmask & 0x2) != 0;
    public bool Glows => (Bitmask & 0x4) != 0;
    public bool HasMark => (Bitmask & 0x8) != 0;

}
/// <summary>Updates a player for a client.</summary>
public record struct UpdatePlayer(int ID, bool Standing, string BodyMode, string Animation, byte AnimationFrame, sbyte FlipDirection, sbyte FlipDirectionLast, Vector2 HeadPos, Vector2 HeadVel, Vector2 ButtPos, Vector2 ButtVel, Vector2 InputDir0, Vector2 InputDir1, Vector2 InputDir2, Vector2 InputDir3, Vector2 InputDir4, Vector2 InputDir5, Vector2 InputDir6, Vector2 InputDir7, Vector2 InputDir8, Vector2 InputDir9, byte InputBitmask0, byte InputBitmask1, byte InputBitmask2, byte InputBitmask3, byte InputBitmask4, byte InputBitmask5, byte InputBitmask6, byte InputBitmask7, byte InputBitmask8, byte InputBitmask9) : IPacket
{
    public static PacketQueue<UpdatePlayer> Queue { get; } = new();
    public static bool Latest(out UpdatePlayer packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<UpdatePlayer> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.UpdatePlayer;
    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();
        Standing = reader.GetBool();
        BodyMode = reader.GetString();
        Animation = reader.GetString();
        AnimationFrame = reader.GetByte();
        FlipDirection = reader.GetSByte();
        FlipDirectionLast = reader.GetSByte();
        HeadPos = reader.GetVec();
        HeadVel = reader.GetVec();
        ButtPos = reader.GetVec();
        ButtVel = reader.GetVec();
        InputDir0 = reader.GetVec();
        InputDir1 = reader.GetVec();
        InputDir2 = reader.GetVec();
        InputDir3 = reader.GetVec();
        InputDir4 = reader.GetVec();
        InputDir5 = reader.GetVec();
        InputDir6 = reader.GetVec();
        InputDir7 = reader.GetVec();
        InputDir8 = reader.GetVec();
        InputDir9 = reader.GetVec();
        InputBitmask0 = reader.GetByte();
        InputBitmask1 = reader.GetByte();
        InputBitmask2 = reader.GetByte();
        InputBitmask3 = reader.GetByte();
        InputBitmask4 = reader.GetByte();
        InputBitmask5 = reader.GetByte();
        InputBitmask6 = reader.GetByte();
        InputBitmask7 = reader.GetByte();
        InputBitmask8 = reader.GetByte();
        InputBitmask9 = reader.GetByte();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);
        writer.Put(Standing);
        writer.Put(BodyMode);
        writer.Put(Animation);
        writer.Put(AnimationFrame);
        writer.Put(FlipDirection);
        writer.Put(FlipDirectionLast);
        writer.Put(HeadPos);
        writer.Put(HeadVel);
        writer.Put(ButtPos);
        writer.Put(ButtVel);
        writer.Put(InputDir0);
        writer.Put(InputDir1);
        writer.Put(InputDir2);
        writer.Put(InputDir3);
        writer.Put(InputDir4);
        writer.Put(InputDir5);
        writer.Put(InputDir6);
        writer.Put(InputDir7);
        writer.Put(InputDir8);
        writer.Put(InputDir9);
        writer.Put(InputBitmask0);
        writer.Put(InputBitmask1);
        writer.Put(InputBitmask2);
        writer.Put(InputBitmask3);
        writer.Put(InputBitmask4);
        writer.Put(InputBitmask5);
        writer.Put(InputBitmask6);
        writer.Put(InputBitmask7);
        writer.Put(InputBitmask8);
        writer.Put(InputBitmask9);

    }
}
/// <summary>TODO!!</summary>
public record struct IntroFly(int ID, string Room) : IPacket
{
    public static PacketQueue<IntroFly> Queue { get; } = new();
    public static bool Latest(out IntroFly packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<IntroFly> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.IntroFly;
    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();
        Room = reader.GetString();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);
        writer.Put(Room);

    }
}
public record struct UpdateFly(int ID) : IPacket
{
    public static PacketQueue<UpdateFly> Queue { get; } = new();
    public static bool Latest(out UpdateFly packet) => Queue.Latest(out _, out packet);
    public static IEnumerable<UpdateFly> All() => Queue.Drain();
    public PacketKind GetKind() => PacketKind.UpdateFly;
    public void Deserialize(NetDataReader reader)
    {
        ID = reader.GetInt();

    }
    public void Serialize(NetDataWriter writer)
    {
        writer.Put(ID);

    }
}
