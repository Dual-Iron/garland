using Common;
using System.Collections.Generic;

namespace Client;

sealed class ClientSession : GameSession
{
    public ClientSession(byte slugcatWorld, int clientPid, RainWorldGame game) : base(game)
    {
        SlugcatWorld = slugcatWorld;
        ClientPid = clientPid;
        RoomRealizer = new(game, this);
    }

    public readonly byte SlugcatWorld;
    public readonly int ClientPid;
    public readonly ClientRoomLogic RoomRealizer;
    public readonly Dictionary<int, PhysicalObject> Objects = new();
    public readonly Dictionary<int, SharedPlayerData> ClientData = new();
    public readonly Dictionary<int, Input> LastInput = new();

    public override void AddPlayer(AbstractCreature player)
    {
        if (player.ID.number == ClientPid) {
            base.AddPlayer(player);
        }
    }

    public SharedPlayerData? GetPlayerData(int pid) => ClientData.TryGetValue(pid, out var data) ? data : null;
    public SharedPlayerData? GetPlayerData(EntityID eid) => GetPlayerData(eid.number);
    public SharedPlayerData? GetPlayerData(AbstractCreature player) => GetPlayerData(player.ID.number);

    public AbstractCreature? MyPlayer => Players.Count > 0 ? Players[0] : null;
    public SharedPlayerData? MyPlayerData => Players.Count > 0 ? ClientData[0] : null;
}

static class ClientExt
{
    public static SharedPlayerData? Data(this Player p)
    {
        if (p.abstractCreature.world.game.session is ClientSession session) {
            return session.GetPlayerData(p.abstractCreature);
        }
        return null;
    }
}
