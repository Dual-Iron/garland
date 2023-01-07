using Common;
using LiteNetLib;

namespace Server;

static class SessionUtils
{
    public static void BroadcastRelevant<T>(this PhysicalObject obj, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : IPacket
    {
        obj.abstractPhysicalObject.BroadcastRelevant(packet, deliveryMethod);
    }
    public static void BroadcastRelevant<T>(this AbstractWorldEntity ent, T packet, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered) where T : IPacket
    {
        ent.world.game.Session().RoomRealizer.BroadcastRelevant(ent.pos.room, packet, deliveryMethod);
    }

    public static SharedPlayerData? Data(this Player player)
    {
        var session = (ServerSession)player.abstractPhysicalObject.world.game.session;

        return session.GetPlayerData(player.abstractPhysicalObject.ID.number);
    }

    public static ServerSession Session(this PhysicalObject o) => (ServerSession)o.Game().session;
    public static ServerSession Session(this RainWorldGame game) => (ServerSession)game.session;
}
