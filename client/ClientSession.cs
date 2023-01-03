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

    public SharedPlayerData GetPlayerData(int pid)
    {
        if (pidToLocalID.TryGetValue(pid, out int localID)) {
            return clientData[localID];
        }
        return new();
    }
    public SharedPlayerData GetPlayerData(EntityID eid) => GetPlayerData(eid.number);
    public SharedPlayerData GetPlayerData(AbstractCreature player) => GetPlayerData(player.ID.number);

    public AbstractCreature MyPlayer => Players[pidToLocalID[ClientPid]];
    public SharedPlayerData MyPlayerData => clientData[pidToLocalID[ClientPid]];
}

static class ClientExt
{
    public static SharedPlayerData Data(this Player p)
    {
        if (p.abstractCreature.world.game.session is ClientSession session) {
            return session.GetPlayerData(p.abstractCreature);
        }
        return null!;
    }
}
