using Common;

namespace Client;

static class ClientUtils
{
    public static SharedPlayerData? Data(this Player p)
    {
        if (p.abstractPhysicalObject.world.game.session is ClientSession session) {
            return session.GetPlayerData(p.abstractCreature);
        }
        return null;
    }

    public static bool IsMyPlayer(this Player p)
    {
        return p.abstractPhysicalObject.world.game.session is ClientSession session && session.MyPlayer == p.abstractPhysicalObject;
    }
}
