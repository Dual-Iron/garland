﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Client -> Server messages are in the 0x100 – 0x1FF range
// Server -> Client messages are in the 0x200 – 0x2FF range
// string end in newline

const string PACKETS = """
# Sent to the server any time the client's input changes.
Input = 0x100 {
    f32 X
    f32 Y
    u8  Bitmask { Jump = 0x1, Throw = 0x2, Pickup = 0x4, Point = 0x8 }
}

# Sent to a client after they join the game and begin a RainWorldGame instance.
EnterSession = 0x200 {
    u8  SlugcatWorldNumber
    u16 RainbowSeed
    str StartingRoom
}

# Sent every two seconds, after `GlobalRain.rainDirectionGetTo` changes, and to newly-connected clients. Flood speed is a constant 0.2.
SyncRain = 0x201 {
    u16 RainTimer
    u16 RainTimerMax
    f32 RainDirection
    f32 RainDirectionGetTo
}

# Sent every two seconds, after `DeathRain.deathRainMode` changes, and to newly-connected clients. Only sent after death rain begins.
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
StringBuilder source = new("using LiteNetLib.Utils;\r\n\r\nnamespace Common;\r\n\r\n");

source.AppendLine("public enum PacketKind : ushort\r\n{");
foreach (var packet in packetKinds) {
    if (packet.Comment != null) {
        source.AppendLine($"    /// <summary>{packet.Comment}</summary>");
    }
    source.AppendLine($"    {packet.Name} = {packet.Value},");
}
source.AppendLine("}\r\n");

foreach (var packet in packetKinds) {
    WritePacketKind(source, packet);
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

#region METHODS
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

    field.Type = ParsePacketFieldType(ref reader);
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

PacketFieldType ParsePacketFieldType(ref PacketSourceReader reader)
{
    return reader.ReadWord() switch {
        "str" => PacketFieldType.Str,
        "bool" => PacketFieldType.Bool,
        "u8" => PacketFieldType.U8,
        "u16" => PacketFieldType.U16,
        "u32" => PacketFieldType.U32,
        "i32" => PacketFieldType.I32,
        "f32" => PacketFieldType.F32,
        _ => throw new("Invalid packet field type")
    };
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

void WritePacketKind(StringBuilder source, PacketKind packet)
{
    if (packet.Comment != null) {
        source.AppendLine($"/// <summary>{packet.Comment}</summary>");
    }

    source.AppendLine($$"""
public record struct {{packet.Name}}({{Params()}}) : IPacket
{
    public static PacketQueue<{{packet.Name}}> Queue { get; } = new();

    public PacketKind Kind => PacketKind.{{packet.Name}};

    public void Deserialize(NetDataReader reader)
    {
{{Deserialize()}}
    }

    public void Serialize(NetDataWriter writer)
    {
{{Serialize()}}
    }{{Bitmask()}}
}

""");

    StringBuilder Params()
    {
        StringBuilder sb = new();
        for (int i = packet.FieldStart; i < packet.FieldEnd; i++) {
            sb.Append($"{fields[i].TypeCsharp()} {fields[i].Name}");
            if (i < packet.FieldEnd - 1) {
                sb.Append(", ");
            }
        }
        return sb;
    }

    string Deserialize()
    {
        StringBuilder sb = new();
        for (int i = packet.FieldStart; i < packet.FieldEnd; i++) {
            sb.AppendLine($"        {fields[i].Name} = reader.{fields[i].DeserializeCall()};");
        }
        return sb.ToString().TrimEnd();
    }

    string Serialize()
    {
        StringBuilder sb = new();
        for (int i = packet.FieldStart; i < packet.FieldEnd; i++) {
            sb.AppendLine($"        writer.Put({fields[i].Name});");
        }
        return sb.ToString().TrimEnd();
    }

    StringBuilder Bitmask()
    {
        StringBuilder sb = new();
        for (int i = packet.FieldStart; i < packet.FieldEnd; i++) {
            if (fields[i].BitmaskStart == fields[i].BitmaskEnd) continue;

            // Generate ToBitmask methods
            sb.Append($"\r\n\r\n    public static {fields[i].TypeCsharp()} To{fields[i].Name}(");
            for (int j = fields[i].BitmaskStart; j < fields[i].BitmaskEnd; j++) {
                sb.Append($"bool {bitmasks[j].Name}");
                if (j < fields[i].BitmaskEnd - 1) {
                    sb.Append(", ");
                }
            }
            sb.Append($")\r\n    {{\r\n        return ({fields[i].TypeCsharp()})(");
            for (int j = fields[i].BitmaskStart; j < fields[i].BitmaskEnd; j++) {
                sb.Append($"({bitmasks[j].Name} ? {bitmasks[j].Value} : 0)");
                if (j < fields[i].BitmaskEnd - 1) {
                    sb.Append(" | ");
                }
            }
            sb.AppendLine(");\r\n    }");
            sb.AppendLine();

            // Generate helper properties
            for (int j = fields[i].BitmaskStart; j < fields[i].BitmaskEnd; j++) {
                sb.Append($"    public bool {bitmasks[j].Name} => ({fields[i].Name} & {bitmasks[j].Value}) != 0;");
                if (j < fields[i].BitmaskEnd - 1) {
                    sb.AppendLine();
                }
            }
        }
        return sb;
    }
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

enum PacketFieldType { Str, Bool, U8, U16, U32, I32, F32, }

struct PacketField
{
    public PacketFieldType Type;
    public string Name;
    public int BitmaskStart;
    public int BitmaskEnd;

    public string TypeCsharp() => Type switch {
        PacketFieldType.Str => "string",
        PacketFieldType.Bool => "bool",
        PacketFieldType.U8 => "byte",
        PacketFieldType.U16 => "ushort",
        PacketFieldType.U32 => "uint",
        PacketFieldType.I32 => "int",
        PacketFieldType.F32 => "float",
        _ => throw new ArgumentException()
    };

    public string DeserializeCall() => Type switch {
        PacketFieldType.Str => "GetString()",
        PacketFieldType.Bool => "GetBool()",
        PacketFieldType.U8 => "GetByte()",
        PacketFieldType.U16 => "GetUShort()",
        PacketFieldType.U32 => "GetUInt()",
        PacketFieldType.I32 => "GetInt()",
        PacketFieldType.F32 => "GetFloat()",
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

    bool Whitespace() => offset < Text.Length && char.IsWhiteSpace(Text[offset]);
}
#endregion