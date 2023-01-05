using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Parse packets into meaningful data
List<PacketKind> packetKinds = new();
List<PacketField> fields = new();
List<BitmaskPart> bitmasks = new();

PacketSourceReader reader = new() { Text = Input.PACKETS };

while (reader.TextRemaining()) {
    packetKinds.Add(ParsePacketKind(ref reader));
}

// Generate C# source
StringBuilder source = new("""
using LiteNetLib;
using LiteNetLib.Utils;
using RWCustom;
using System;
using System.Collections.Generic;
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

// Find `garland` directory and move file to `garland/common`...
DirectoryInfo dir = new(Path.GetFullPath("."));
while (dir.Parent != null) {
    if (dir.Name == "garland") {
        File.Move("Packets.generated.cs", Path.Combine(dir.FullName, "common", "Packets.generated.cs"), overwrite: true);

        Console.WriteLine("File successfully generated and moved to `common` directory.");

        return;
    }
    dir = dir.Parent;
}

// ...or print an error
Console.Error.WriteLine("File successfully generated, but not moved to `common` directory.");

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
    public static IEnumerable<{{packet.Name}}> All() => Queue.Drain();

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
        "u8[]" => "byte[]",
        "u16" => "ushort",
        "u32" => "uint",
        "i32" => "int",
        "f32" => "float",
        "vec" => "Vector2",
        "vec[]" => "Vector2[]",
        "ivec" => "IntVector2",
        "ivec[]" => "IntVector2[]",
        _ => throw new ArgumentException()
    };

    public string DeserializeCall() => Type switch {
        "str" => "GetString()",
        "bool" => "GetBool()",
        "u8" => "GetByte()",
        "u8[]" => "GetByteArray()",
        "u16" => "GetUShort()",
        "u32" => "GetUInt()",
        "i32" => "GetInt()",
        "f32" => "GetFloat()",
        "vec" => "GetVec()",
        "vec[]" => "GetVecArray()",
        "ivec" => "GetIVec()",
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
