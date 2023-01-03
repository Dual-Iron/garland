using System.Collections.Generic;

namespace Client;

sealed class ClientSession : GameSession
{
    public ClientSession(byte slugcatWorld, int clientPid, RainWorldGame game) : base(game)
    {
        SlugcatWorld = slugcatWorld;
        ClientPid = clientPid;
    }

    public byte SlugcatWorld { get; }
    public int ClientPid { get; }

    readonly Dictionary<int, int> pidToLocalID = new();
    readonly List<SharedPlayerData> clientData = new();

    public override void AddPlayer(AbstractCreature player)
    {
        if (player.ID.number >= 0) {
            pidToLocalID[player.ID.number] = Players.Count;

            clientData.Add(new());
        }

        base.AddPlayer(player);
    }

    public SharedPlayerData? GetPlayerData(int pid) => pidToLocalID.TryGetValue(pid, out int localID) ? clientData[localID] : null;
    public SharedPlayerData? GetPlayerData(EntityID eid) => GetPlayerData(eid.number);
    public SharedPlayerData? GetPlayerData(AbstractCreature player) => GetPlayerData(player.ID.number);

    public AbstractCreature? MyPlayer => pidToLocalID.TryGetValue(ClientPid, out int id) ? Players[id] : null;
    public SharedPlayerData? MyPlayerData => pidToLocalID.TryGetValue(ClientPid, out int id) ? clientData[id] : null;
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
