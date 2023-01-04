using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// 0x100–0x1FF: Packets sent FROM client TO server
// 0x200–0x3FF: Packets sent FROM server TO client
//   0x200: Creature/object introductions, misc packets
//   0x300: Creature/object updates

// TODO non-player objects lol

const string PACKETS = """
# Sent to the server any time the client's input changes.
Input = 0x100 {
    vec Dir
    u8  Bitmask { Jump = 0x1, Throw = 0x2, Pickup = 0x4, Point = 0x8 }
}

# Sent to clients joining a game session.
EnterSession = 0x200 {
    u8  SlugcatWorld
    u16 RainbowSeed
    i32 ClientPid
    str StartingRoom
}

# Sent every 15 seconds, after `GlobalRain.rainDirectionGetTo` changes, and after a client joins. Flood speed is a constant 0.2.
SyncRain = 0x201 {
    u16 RainTimer
    u16 RainTimerMax
    f32 RainDirection
    f32 RainDirectionGetTo
}

# Sent every two seconds, after `DeathRain.deathRainMode` changes, and after a client joins. Only sent after death rain begins.
SyncDeathRain = 0x202 {
    u8  DeathRainMode
    f32 TimeInThisMode
    f32 Progression
    f32 CalmBeforeSunlight
}

# Sent after each time AntiGravity toggles on or off. Progress is set to 0 each time the packet is received.
SyncAntiGrav = 0x203 {
    bool On
    u16  Counter
    f32  From
    f32  To
}

# Tells a client to realize a room if it hasn't already.
RealizeRoom = 0x204 {
    i32 Index
}

# Tells a client to abtractize a room if it hasn't already. TODO (low-priority)
AbstractizeRoom = 0x205 {
    i32 Index
}

# Tells a client to destroy an object if it exists. TODO
DestroyObject = 0x210 {
    i32 ID
}

# Tells a client that a creature is inside a shortcut. TODO (high-priority)
SyncShortcut = 0x211 {
    i32    CreatureID
    i32    RoomID
    i32    EntranceNode
    i32    Wait
    ivec[] Positions
}

# Introduces a player to the client. TODO (next)
IntroPlayer = 0x220 {
    i32  ID
    u8   SkinR
    u8   SkinG
    u8   SkinB
    f32  RunSpeed
    f32  PoleClimbSpeed
    f32  CorridorClimbSpeed
    f32  BodyWeight
    f32  Lungs
    f32  Loudness
    f32  VisBonus
    f32  Stealth
    i32  ThrowingSkill
    bool Ill
}

# Updates a player for a client.
UpdatePlayer = 0x300 {
    i32 Room
    vec HeadPos
    vec ButtPos
    vec InputDir
    u8  InputBitmask { Jump = 0x1, Throw = 0x2, Pickup = 0x4, Point = 0x8 }
}

""";

// Parse packets into meaningful data
List<PacketKind> packetKinds = new();
List<PacketField> fields = new();
List<BitmaskPart> bitmasks = new();

PacketSourceReader reader = new() { Text = PACKETS };

while (reader.TextRemaining()) {
    packetKinds.Add(ParsePacketKind(ref reader));
}

// Generate C# source
StringBuilder source = new("""
using LiteNetLib;
using LiteNetLib.Utils;
using RWCustom;
using System;
using UnityEngine;

namespace Common;


""");

WritePacketKindEnum();

WritePacketsClass();

foreach (var packet in packetKinds) {
    WritePacketKind(packet);
}

// Create file
File.WriteAllText("Packets.generated.cs", source.ToString());

try {
    File.Copy("Packets.generated.cs", "../../../../lib/Packets.generated.cs", overwrite: true);
    Console.WriteLine("File successfully generated and copied to `lib` directory.");
}
catch {
    Console.WriteLine("File successfully generated. Please copy to `lib` directory manually.");
}

#region PARSE METHODS
PacketKind ParsePacketKind(ref PacketSourceReader reader)
{
    PacketKind kind;

    reader.TryReadComment(out kind.Comment); // # Sent to the server any time the client's input changes.
    kind.Name = reader.ReadWord(); // Input
    reader.ReadWord(); // =
    kind.Value = reader.ReadWord(); // 0x100
    reader.ReadWord(); // {

    kind.FieldStart = fields.Count;
    while (!reader.MatchWord("}")) {
        fields.Add(ParsePacketField(ref reader));
    }
    kind.FieldEnd = fields.Count;

    return kind;
}

PacketField ParsePacketField(ref PacketSourceReader reader)
{
    PacketField field;

    field.Type = reader.ReadWord();
    field.Name = reader.ReadWord();

    field.BitmaskStart = bitmasks.Count;
    if (reader.MatchWord("{")) {
        while (!reader.MatchWord("}")) {
            bitmasks.Add(ParseBitmask(ref reader));
        }
    }
    field.BitmaskEnd = bitmasks.Count;

    return field;
}

BitmaskPart ParseBitmask(ref PacketSourceReader reader)
{
    BitmaskPart bitmask;

    bitmask.Name = reader.ReadWord();
    reader.ReadWord(); // =
    bitmask.Value = reader.ReadWord();
    reader.MatchWord(",");

    return bitmask;
}
#endregion

#region WRITE METHODS
void WritePacketKindEnum()
{
    StringBuilder values = new();
    foreach (var packet in packetKinds) {
        if (packet.Comment != null) {
            values.AppendLine($"    /// <summary>{packet.Comment}</summary>");
        }
        values.AppendLine($"    {packet.Name} = {packet.Value},");
    }
    source.AppendLine($$"""
public enum PacketKind : ushort
{
{{values}}
}

""");
}

void WritePacketsClass()
{
    StringBuilder cases = new();
    foreach (var packet in packetKinds) {
        cases.AppendLine($"                case {packet.Value}: {packet.Name}.Queue.Enqueue(sender, data.Read<{packet.Name}>()); break;");
    }
    source.AppendLine($$"""
public static partial class Packets
{
    public static void QueuePacket(NetPeer sender, NetPacketReader data, Action<string> error)
    {
        try {
            ushort type = data.GetUShort();

            switch (type) {
{{cases}}
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

""");
}

void WritePacketKind(PacketKind packet)
{
    StringBuilder parameters = new();
    for (int i = packet.FieldStart; i < packet.FieldEnd; i++) {
        parameters.Append($"{fields[i].TypeCsharp()} {fields[i].Name}");
        if (i < packet.FieldEnd - 1) {
            parameters.Append(", ");
        }
    }

    StringBuilder deserializeContents = new();
    for (int i = packet.FieldStart; i < packet.FieldEnd; i++) {
        deserializeContents.AppendLine($"        {fields[i].Name} = reader.{fields[i].DeserializeCall()};");
    }

    StringBuilder serializeContents = new();
    for (int i = packet.FieldStart; i < packet.FieldEnd; i++) {
        serializeContents.AppendLine($"        writer.Put({fields[i].Name});");
    }

    StringBuilder bitmaskMethods = new();
    for (int i = packet.FieldStart; i < packet.FieldEnd; i++) {
        if (fields[i].BitmaskStart == fields[i].BitmaskEnd) continue;

        // Generate ToBitmask methods
        bitmaskMethods.Append($"\r\n\r\n    public static {fields[i].TypeCsharp()} To{fields[i].Name}(");
        for (int j = fields[i].BitmaskStart; j < fields[i].BitmaskEnd; j++) {
            bitmaskMethods.Append($"bool {bitmasks[j].Name}");
            if (j < fields[i].BitmaskEnd - 1) {
                bitmaskMethods.Append(", ");
            }
        }
        bitmaskMethods.Append($")\r\n    {{\r\n        return ({fields[i].TypeCsharp()})(");
        for (int j = fields[i].BitmaskStart; j < fields[i].BitmaskEnd; j++) {
            bitmaskMethods.Append($"({bitmasks[j].Name} ? {bitmasks[j].Value} : 0)");
            if (j < fields[i].BitmaskEnd - 1) {
                bitmaskMethods.Append(" | ");
            }
        }
        bitmaskMethods.AppendLine(");\r\n    }");
        bitmaskMethods.AppendLine();

        // Generate helper properties
        for (int j = fields[i].BitmaskStart; j < fields[i].BitmaskEnd; j++) {
            bitmaskMethods.AppendLine($"    public bool {bitmasks[j].Name} => ({fields[i].Name} & {bitmasks[j].Value}) != 0;");
        }
    }

    if (packet.Comment != null) {
        source.AppendLine($"/// <summary>{packet.Comment}</summary>");
    }

    source.AppendLine($$"""
public record struct {{packet.Name}}({{parameters}}) : IPacket
{
    public static PacketQueue<{{packet.Name}}> Queue { get; } = new();

    public static bool Latest(out {{packet.Name}} packet) => Queue.Latest(out _, out packet);

    public PacketKind GetKind() => PacketKind.{{packet.Name}};

    public void Deserialize(NetDataReader reader)
    {
{{deserializeContents}}
    }

    public void Serialize(NetDataWriter writer)
    {
{{serializeContents}}
    }{{bitmaskMethods}}
}

""");
}
#endregion

#region STRUCTS
struct PacketKind
{
    public string Value;
    public string Name;
    public string? Comment;
    public int FieldStart;
    public int FieldEnd;
}

struct PacketField
{
    public string Type;
    public string Name;
    public int BitmaskStart;
    public int BitmaskEnd;

    public string TypeCsharp() => Type switch {
        "str" => "string",
        "bool" => "bool",
        "u8" => "byte",
        "u16" => "ushort",
        "u32" => "uint",
        "i32" => "int",
        "f32" => "float",
        "vec" => "Vector2",
        "ivec[]" => "IntVector2[]",
        _ => throw new ArgumentException()
    };

    public string DeserializeCall() => Type switch {
        "str" => "GetString()",
        "bool" => "GetBool()",
        "u8" => "GetByte()",
        "u16" => "GetUShort()",
        "u32" => "GetUInt()",
        "i32" => "GetInt()",
        "f32" => "GetFloat()",
        "vec" => "GetVec()",
        "ivec[]" => "GetIVecArray()",
        _ => throw new ArgumentException()
    };
}

struct BitmaskPart
{
    public string Value;
    public string Name;
}

struct PacketSourceReader
{
    public string Text;

    int offset;

    bool Whitespace() => offset < Text.Length && char.IsWhiteSpace(Text[offset]);

    public bool TextRemaining()
    {
        while (Whitespace()) offset += 1;
        return offset < Text.Length;
    }

    public bool TryReadComment(out string comment)
    {
        while (Whitespace()) offset += 1;

        // Scan entire line if the first char is #
        if (Text[offset] == '#') {
            int start = ++offset;
            while (Text[offset] != '\n') offset += 1;
            comment = Text[start..offset].Trim();
            return true;
        }

        comment = null!;
        return false;
    }

    public bool MatchWord(string word)
    {
        if (PeekWord() == word) {
            ReadWord();
            return true;
        }
        return false;
    }

    public string PeekWord()
    {
        int preOffset = offset;
        string word = ReadWord();
        offset = preOffset;
        return word;
    }

    public string ReadWord()
    {
        if (Text[offset] == ',') {
            offset += 1;
            return Text[(offset - 1)..offset];
        }
        // Skip whitespace, then read until whitespace or comma
        while (Whitespace()) offset += 1;
        int start = offset;
        while (!Whitespace() && Text[offset] != ',') offset += 1;
        return Text[start..offset];
    }
}
#endregion
