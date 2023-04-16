using Common;
using System.Collections.Generic;

namespace Client;

sealed class ClientSession : GameSession
{
    public ClientSession(SlugcatStats.Name slugcatWorld, int clientPid, RainWorldGame game) : base(game)
    {
        SlugcatWorld = slugcatWorld;
        ClientPid = clientPid;
        RoomRealizer = new(game, this);
    }

    public readonly SlugcatStats.Name SlugcatWorld;
    public readonly int ClientPid;
    public readonly ClientRoomLogic RoomRealizer;
    public readonly Dictionary<int, PhysicalObject> Objects = new();
    public readonly Dictionary<int, SharedPlayerData> ClientData = new();

    public readonly Dictionary<int, Input> PlayerLastInput = new();
    public readonly Dictionary<int, UpdatePlayer> UpdatePlayer = new();

    public override void AddPlayer(AbstractCreature player)
    {
        if (player.ID() == ClientPid) {
            base.AddPlayer(player);

            Main.Log.LogDebug($"Added my player ({player.ID()}) to session");
        }
    }

    public SharedPlayerData? GetPlayerData(int pid) => ClientData.TryGetValue(pid, out var data) ? data : null;
    public SharedPlayerData? GetPlayerData(EntityID eid) => GetPlayerData(eid.number);
    public SharedPlayerData? GetPlayerData(AbstractCreature player) => GetPlayerData(player.ID.number);

    public AbstractCreature? MyPlayer => Players.Count > 0 ? Players[0] : null;
    public SharedPlayerData? MyPlayerData => Players.Count > 0 ? ClientData[0] : null;
}
