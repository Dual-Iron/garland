using Common;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rng = UnityEngine.Random;
using static UnityEngine.Mathf;

namespace Server;

sealed class ServerSession : GameSession
{
    record struct PeerData(string Hash, int Pid);

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
        int seed = Rng.seed;
        Rng.seed = hash.GetHashCode();

        float hue = Pow(Rng.value, 3f); // Pow to make hotter colors more common
        float saturation = Lerp(1f, 0.8f, Rng.value * Rng.value);
        float luminosity = Lerp(1f, 0.5f, Rng.value * Rng.value);

        Color color = RXColor.ColorFromHSL(hue, saturation, luminosity);

        SlugcatStats stats = new(slugcatNumber: 0, malnourished: false);

        float fat = Lerp(-0.1f, 0.15f, Rng.value);
        float speed = Lerp(-0.05f, 0.1f, Rng.value);
        float sneakiness = Lerp(-0.15f, 0.15f, Rng.value);

        stats.runspeedFac += speed - fat * 0.5f;
        stats.poleClimbSpeedFac += speed * 1.5f - fat * 0.5f;
        stats.corridorClimbSpeedFac += speed - fat * 0.5f;
        stats.generalVisibilityBonus -= sneakiness;
        stats.visualStealthInSneakMode += sneakiness;
        stats.loudnessFac += fat * 2 - sneakiness;
        stats.lungsFac += Lerp(-0.2f, 0.1f, Rng.value - speed);
        stats.bodyWeightFac += fat;

        stats.foodToHibernate += (int)(speed * 30 + fat * 5);
        stats.maxFood += (int)(fat * 30);

        if (stats.maxFood < stats.foodToHibernate + 1)
            stats.maxFood = stats.foodToHibernate + 1;

        static string Offset(float value) => value < 0 ? $"- {-value:F2}" : $"+ {value:F2}";

        Main.Log.LogDebug($"""
            Generated stats for player {pid}: fat {fat:P0}, speed {speed:P0}, sneakiness {sneakiness:P0}
            run speed    {stats.runspeedFac,6:P0}
            pole speed   {stats.poleClimbSpeedFac,6:P0}
            tunnel speed {stats.corridorClimbSpeedFac,6:P0}
            loudness     {stats.loudnessFac,6:P0}
            lungs        {stats.lungsFac,6:P0}
            weight       {stats.bodyWeightFac,6:P0}
            vis bonus    {Offset(stats.generalVisibilityBonus),6}
            stealth      {Offset(stats.visualStealthInSneakMode - 0.5f),6}
            """);

        Rng.seed = seed;
        return new() {
            Stats = stats,
            SkinColor = color,
            EatsMeat = stats.foodToHibernate > 6,
            Glows = false,
            HasMark = false,
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

    /// <summary>Returns true if any connected peers are already associated with the given player hash.</summary>
    public bool AnyPeerConnectedTo(string hash) => peers.Values.Any(v => v.Hash == hash);

    /// <summary>Called when a peer joins the game and needs a new AbstractCreature to represent them.</summary>
    public AbstractCreature Join(NetPeer peer, string hash)
    {
        var crit = GetOrCreatePlayer(hash);

        // If this peer is new, add em.
        if (!peers.TryGetValue(peer.Id, out var peerData) || peerData.Hash != hash) {
            peers[peer.Id] = new PeerData(hash, hashToPid[hash]);
        }

        return crit;
    }

    public void Leave(NetPeer peer)
    {
        peers.Remove(peer.Id);
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
