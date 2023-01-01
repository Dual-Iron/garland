using LiteNetLib;
using LiteNetLib.Utils;
using System.IO;

namespace Common;

public static class Utils
{
    private static readonly NetDataWriter writer = new();

    private static RainWorld? rw;
    public static RainWorld Rw => rw ??= UnityEngine.Object.FindObjectOfType<RainWorld>();

    public static bool DirExistsAt(params string[] path)
    {
        return Directory.Exists(BepInEx.Utility.CombinePaths(path));
    }

    public static void Send<T>(this NetPeer peer, T value, DeliveryMethod deliveryMethod) where T : IPacket
    {
        writer.Reset();
        writer.Put((ushort)value.GetKind());
        writer.Put(value);
        peer.Send(writer, deliveryMethod);
    }

    public static T Read<T>(this NetDataReader reader) where T : struct, INetSerializable
    {
        T value = default;
        value.Deserialize(reader);
        return value;
    }
}
