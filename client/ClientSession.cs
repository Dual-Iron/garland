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

    public readonly Dictionary<int, Input> PlayerLastInput = new();
    public readonly Dictionary<int, UpdatePlayer> UpdatePlayer = new();

    public override void AddPlayer(AbstractCreature player)
    {
        if (player.ID.number == ClientPid) {
            base.AddPlayer(player);

            Main.Log.LogDebug($"Added my player ({player.ID.number}) to session");
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
        if (p.abstractPhysicalObject.world.game.session is ClientSession session) {
            return session.GetPlayerData(p.abstractCreature);
        }
        return null;
    }

    public static int Pid(this Player p) => p.abstractPhysicalObject.ID.number;

    public static bool IsMyPlayer(this Player p)
    {
        return p.abstractPhysicalObject.world.game.session is ClientSession session && session.MyPlayer == p.abstractPhysicalObject;
    }
}
