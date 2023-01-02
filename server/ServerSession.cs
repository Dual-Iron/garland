using Common;
using LiteNetLib;
using System.Collections.Generic;
using System.Linq;

namespace Server;

sealed class ServerSession : GameSession
{
    struct PeerData
    {
        public string Hash;
        public int Pid;
    }

    public ServerSession(byte slugcatWorld, RainWorldGame game) : base(game)
    {
        SlugcatWorld = slugcatWorld;
    }

    public byte SlugcatWorld { get; }

    // TODO: Save and load PlayersData and hashToPid. Set Players just after PlayersData is finished loading.

    // Each player has one Player ID (PID).
    // They start at 0 and increase from there, so they can be accessed in a list.
    // All players in this list are represented as abstract creatures. Even disconnected players are present, but unconscious. Disconnected players might not realize rooms, though.
    // Saved to disk.
    readonly List<PlayerData> playersData = new();

    // TODO: A player's hash will be given by SHA256(username, password).
    // Saved to disk.
    readonly Dictionary<string, int> hashToPid = new();

    // Decided whenever a peer logs in.
    // Ephemeral.
    readonly Dictionary<int, PeerData> peers = new();

    /// <summary>Fetches a player from their hash, or creates a new player if they don't exist.</summary>
    private AbstractCreature GetOrCreatePlayer(string hash)
    {
        // Create player's PID if it doesn't exist
        if (!hashToPid.TryGetValue(hash, out int pid)) {
            hashToPid[hash] = pid = playersData.Count;

            playersData.Add(PlayerData.Generate(this, hash));

            // TODO: then save immediately

            // Create new abstract player and add it
            PlayerData data = playersData[pid];
            EntityID id = new(-1, pid); // TODO: other objects' IDs start at 1000, so this is a safe bet.. for now. Make IDs start at 100,000 later.
            AbstractCreature player = new(game.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, data.pos, id);
            player.state = new PlayerState(player, pid, 0, false);

            base.AddPlayer(player);
        }

        return Players[pid];
    }

    /// <summary>Called when a peer joins the game and needs a new AbstractCreature to represent them.</summary>
    public AbstractCreature Join(NetPeer peer, string hash)
    {
        var crit = GetOrCreatePlayer(hash);

        // If peer isn't already cached, or is cached under the wrong hash, cache it.
        if (!peers.TryGetValue(peer.Id, out var peerData) || peerData.Hash != hash) {
            peers[peer.Id] = new PeerData { Hash = hash, Pid = hashToPid[hash] };
        }

        return crit;
    }

    public override void AddPlayer(AbstractCreature player)
    {
        if (player.PlayerState().playerNumber != -1) {
            throw new System.ArgumentException($"Players with no associated client must have a playerNumber of -1.");
        }
        base.AddPlayer(player);
    }

    public PlayerData GetPlayerData(int pid) => playersData[pid];
    public PlayerData GetPlayerData(EntityID eid) => playersData[eid.number];
    public PlayerData GetPlayerData(AbstractCreature player) => playersData[player.ID.number];
    public PlayerData GetPlayerData(NetPeer peer)
    {
        if (!peers.TryGetValue(peer.Id, out PeerData data)) {
            throw new System.ArgumentException("Peer does not have any associated player; call ServerSession::Join before using ServerSession::GetPlayer.");
        }
        return playersData[data.Pid];
    }

    public AbstractCreature GetPlayer(NetPeer peer)
    {
        if (!peers.TryGetValue(peer.Id, out PeerData data)) {
            throw new System.ArgumentException("Peer does not have any associated player; call ServerSession::Join before using ServerSession::GetPlayer.");
        }
        return Players[data.Pid];
    }
    public AbstractCreature GetPlayer(EntityID eid)
    {
        if (eid.number >= 0 && eid.number < Players.Count) {
            return Players[eid.number];
        }
        return Players.First(p => p.ID == eid);
    }
}

static class SessionExt
{
    public static PlayerData Data(this Player player)
    {
        var session = (ServerSession)player.abstractCreature.world.game.session;

        return session.GetPlayerData(player.abstractCreature.ID.number);
    }
}

sealed class PlayerData
{
    public WorldCoordinate pos;
    public SlugcatStats stats;

    private PlayerData(WorldCoordinate pos, SlugcatStats stats)
    {
        this.pos = pos;
        this.stats = stats;
    }

    public static PlayerData Generate(ServerSession session, string hash)
    {
        return new(
            new WorldCoordinate(session.game.world.GetAbstractRoom(ServerConfig.StartingRoom).index, 5, 5, -1),
            new SlugcatStats(slugcatNumber: 0, malnourished: false)
        );
    }
}
