using Common;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rng = UnityEngine.Random;
using static UnityEngine.Mathf;
using System.Security.Cryptography;
using System.Text;

namespace Server;

sealed class ServerSession : GameSession
{
    record struct PeerData(string Name, int Pid);

    private static string GetHash(string name, string password)
    {
        string injectiveCombination = $"{name.Length}{name}{password.Length}{password}";
        byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(injectiveCombination));
        string hash = Convert.ToBase64String(hashBytes);
        return hash;
    }

    public ServerSession(byte slugcatWorld, RainWorldGame game) : base(game)
    {
        SlugcatWorld = slugcatWorld;
        RoomRealizer = new(game, this);
    }

    public readonly byte SlugcatWorld;
    public readonly ServerRoomLogic RoomRealizer;
    public readonly List<Common.Input> LastInput = new();
    public readonly ServerSaveState Save = new();

    // Decided whenever a peer logs in.
    readonly Dictionary<int, PeerData> peers = new();

    private SharedPlayerData CreateNewPlayerData(string hash, int pid)
    {
        int seed = Rng.seed;
        Rng.seed = hash.GetHashCode();

        float hue = Pow(Rng.value, 3f); // Pow to make hotter colors more common
        float saturation = Lerp(1f, 0.8f, Rng.value * Rng.value);
        float luminosity = Lerp(1f, 0.5f, Rng.value * Rng.value);

        Color color = RXColor.ColorFromHSL(hue, saturation, luminosity);

        float fat = Lerp(-0.1f, 0.15f, Rng.value);
        float speed = Lerp(-0.05f, 0.1f, Rng.value);
        float sneakiness = Lerp(-0.15f, 0.15f, Rng.value);

        Rng.seed = seed;

        int foodSleep = 4 + (int)(speed * 30 + fat * 5);
        int foodMax = Max(7 + (int)(fat * 30), foodSleep + 1);

        Main.Log.LogDebug($"Generated stats for player {pid}: fat {fat:P0}, speed {speed:P0}, sneakiness {sneakiness:P0}");

        return new SharedPlayerData() {
            SkinColor = color,

            FoodMax = (byte)foodMax,
            FoodSleep = (byte)foodSleep,
            RunSpeed = 1 + speed - fat * 0.5f,
            PoleClimbSpeed = 1 + speed * 1.5f - fat * 0.5f,
            CorridorClimbSpeed = 1 + speed - fat * 0.5f,
            Weight = 1 + fat,
            VisBonus = speed - sneakiness,
            SneakStealth = 0.5f + sneakiness,
            Loudness = 1 + fat * 2 - sneakiness,
            LungWeakness = 1 - speed * 2,
            Ill = false,

            EatsMeat = foodSleep > 6,
            Glows = false,
            HasMark = false,
        };
    }

    private WorldCoordinate CreateNewPlayerPos()
    {
        return new(game.world.GetAbstractRoom(ServerConfig.StartingRoom).index, 5, 5, -1);
    }

    /// <summary>Fetches a player from their hash, or creates a new player if they don't exist.</summary>
    private AbstractCreature GetOrCreatePlayer(string name, string password)
    {
        // Create player's PID if it doesn't exist
        if (!Save.nameToPid.TryGetValue(name, out int pid)) {
            string hash = GetHash(name, password);

            Save.nameToPid[name] = pid = Save.playerData.Count;
            Save.playerData.Add(new(CreateNewPlayerData(hash, pid), hash));
            LastInput.Add(default);

            // Create new abstract player and add it
            EntityID id = new(-1, pid); // TODO: other objects' IDs start at 1000, so this is a safe bet.. for now. Make IDs start at 100,000 later.
            AbstractCreature player = new(game.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, CreateNewPlayerPos(), id);
            player.state = new PlayerState(player, pid, 0, false);

            base.AddPlayer(player);
        }

        return Players[pid];
    }

    /// <summary>Returns true if any connected peers are already associated with the given player hash.</summary>
    public bool AnyPeerConnected(string name) => peers.Values.Any(v => v.Name == name);

    /// <summary>Returns true if the password for a given username matches.</summary>
    public bool OkPassword(string name, string password)
    {
        return !Save.nameToPid.TryGetValue(name, out int pid) || Save.playerData[pid].Hash == GetHash(name, password);
    }

    /// <summary>Called when a peer joins the game and needs a new AbstractCreature to represent them.</summary>
    public AbstractCreature Join(NetPeer peer, string name, string password)
    {
        var crit = GetOrCreatePlayer(name, password);

        // If this peer is new, add em.
        if (!peers.TryGetValue(peer.Id, out var peerData) || peerData.Name != name) {
            peers[peer.Id] = new PeerData(name, Save.nameToPid[name]);
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

    public SharedPlayerData? GetPlayerData(int pid) => pid >= 0 && pid < Save.playerData.Count ? Save.playerData[pid].Shared : null;
    public SharedPlayerData? GetPlayerData(EntityID eid) => GetPlayerData(eid.number);
    public SharedPlayerData? GetPlayerData(AbstractCreature player) => GetPlayerData(player.ID.number);
    public SharedPlayerData? GetPlayerData(NetPeer peer) => peers.TryGetValue(peer.Id, out PeerData data) ? Save.playerData[data.Pid].Shared : null;

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
