using Common;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        RoomRealizer = new(game, this);
    }

    public readonly byte SlugcatWorld;
    public readonly ServerRoomLogic RoomRealizer;
    public readonly List<Common.Input> LastInput = new();

    // TODO: Save and load hashToPid, serverData, pos, and playerState for each player at the end/start of each session.
    // Players that don't connect for the entire cycle just don't lose any hunger and stay put.

    // Each player has one Player ID (PID).
    // They start at 0 and increase from there, so they can be accessed in a list.
    // All players in this list are represented as abstract creatures. Even disconnected players are present, but unconscious. Disconnected players might not realize rooms, though.
    // Saved to disk.
    readonly List<SharedPlayerData> serverData = new();

    // TODO: A player's hash will be given by SHA256(username, password).
    // Saved to disk.
    readonly Dictionary<string, int> hashToPid = new();

    // Decided whenever a peer logs in.
    // Ephemeral.
    readonly Dictionary<int, PeerData> peers = new();

    private SharedPlayerData CreateNewPlayerData(string hash, int pid)
    {
        int hashCode = hash.GetHashCode();

        float hue = Mathf.Pow(Utils.SeededRandom(hashCode), 3f); // Pow to make hotter colors more common
        float saturation = 0.6f + 0.4f * Utils.SeededRandom(hashCode);
        float luminosity = 0.8f + 0.2f * Utils.SeededRandom(hashCode);

        Color color = RXColor.ColorFromHSL(hue, saturation, luminosity);

        return new() {
            Stats = new SlugcatStats(slugcatNumber: 0, malnourished: false),
            SkinColor = color,
        };
    }

    private WorldCoordinate CreateNewPlayerPos(string hash, int pid)
    {
        return new(game.world.GetAbstractRoom(ServerConfig.StartingRoom).index, 5, 5, -1);
    }

    /// <summary>Fetches a player from their hash, or creates a new player if they don't exist.</summary>
    private AbstractCreature GetOrCreatePlayer(string hash)
    {
        // Create player's PID if it doesn't exist
        if (!hashToPid.TryGetValue(hash, out int pid)) {
            hashToPid[hash] = pid = serverData.Count;

            serverData.Add(CreateNewPlayerData(hash, pid));
            LastInput.Add(default);

            // TODO: then save immediately

            // Create new abstract player and add it
            EntityID id = new(-1, pid); // TODO: other objects' IDs start at 1000, so this is a safe bet.. for now. Make IDs start at 100,000 later.
            AbstractCreature player = new(game.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, CreateNewPlayerPos(hash, pid), id);
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

    public void SendObjectUpdate<T>(PhysicalObject o, T packet) where T : IPacket
    {
        RoomRealizer.ObjectUpdate(o, packet);
    }

    public override void AddPlayer(AbstractCreature player)
    {
        if (player.PlayerState().playerNumber == -1) {
            base.AddPlayer(player);
        }
        else {
            throw new ArgumentException($"Players with no associated client must have a playerNumber of -1.");
        }
    }

    public SharedPlayerData? GetPlayerData(int pid) => pid >= 0 && pid < serverData.Count ? serverData[pid] : null;
    public SharedPlayerData? GetPlayerData(EntityID eid) => GetPlayerData(eid.number);
    public SharedPlayerData? GetPlayerData(AbstractCreature player) => GetPlayerData(player.ID.number);
    public SharedPlayerData? GetPlayerData(NetPeer peer) => peers.TryGetValue(peer.Id, out PeerData data) ? serverData[data.Pid] : null;

    public AbstractCreature? GetPlayer(NetPeer peer) => peers.TryGetValue(peer.Id, out PeerData data) ? Players[data.Pid] : null;
}

static class SessionExt
{
    public static SharedPlayerData? Data(this Player player)
    {
        var session = (ServerSession)player.abstractPhysicalObject.world.game.session;

        return session.GetPlayerData(player.abstractPhysicalObject.ID.number);
    }

    public static ServerSession Session(this PhysicalObject o) => (ServerSession)o.Game().session;
    public static ServerSession Session(this RainWorldGame game) => (ServerSession)game.session;
}
