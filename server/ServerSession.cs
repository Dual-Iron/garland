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

// TODO: Make this derive from StoryGameSession. Same for client. Will save me a LOT of headache.
// Analyze for StoryGameSession and IsStoryGameSession - ex LizardAI::GiftReceived and various creature relationships are weird
// Maybe look into an optional gossip-based reputation system instead of community relationships
// Also, add a configurable chance for lineages to loop around
sealed class ServerSession : StoryGameSession
{
    record struct PeerData(string Name, int Pid);

    private static string GetHash(string name, string password)
    {
        string injectiveCombination = $"{name.Length}{name}{password.Length}{password}";
        byte[] hashBytes = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(injectiveCombination));
        string hash = Convert.ToBase64String(hashBytes);
        return hash;
    }

    public ServerSession(SlugcatStats.Name slugcatWorld, RainWorldGame game) : base(slugcatWorld, game)
    {
        RoomRealizer = new(game, this);
        playerSessionRecords[0] = new(0);
    }

    public readonly ServerRoomLogic RoomRealizer;
    public readonly List<Input> LastInput = new();
    public readonly ServerSaveState Save = new();

    // Decided whenever a peer logs in.
    readonly Dictionary<int, PeerData> peers = new();

    private SharedPlayerData CreateNewPlayerData(string hash, int pid)
    {
        Rng.State state = Rng.state;
        Rng.InitState(hash.GetHashCode());

        float hue = Rng.value * Rng.value;
        float saturation = Lerp(1f, 0.80f, Rng.value);
        float luminosity = Lerp(1f, 0.60f, Rng.value);

        Color32 skinColor = RXColor.ColorFromHSL(hue, saturation, luminosity);

        hue = Rng.value;
        saturation = Lerp(1f, 0f, Rng.value * Rng.value); // Make gray eyes rarer
        luminosity = Lerp(0f, 0.32f, Rng.value);

        Color32 eyeColor = RXColor.ColorFromHSL(hue, saturation, luminosity);
        if (eyeColor.b == 0)
            eyeColor.b = 1; // prevent pureblack

        float fat = Pow(Lerp(-1, +1, Rng.value), 2);
        float speed = Pow(Lerp(-1, +1, Rng.value), 2);
        float charm = Pow(Lerp(-1, +1, Rng.value), 2);

        Rng.state = state;

        int foodSleep = Max(2,       4 + (int)(speed * 2.4f + fat * 1.4f));
        int foodMax = Max(foodSleep, 7 + (int)(fat * 4.5f));

        if (foodSleep < 2) foodSleep = 2;
        if (foodMax < foodSleep) foodMax = foodSleep;

        static string Fmt(float stat) => stat > 0 ? $"+{RoundToInt(stat * 100)}%" : $"{RoundToInt(stat * 100)}%";
        static string FmtColor(Color32 color) => $"0x{color.r:X}{color.g:X}{color.b:X}";

        Main.Log.LogDebug($"Player#{pid} stats: fat {Fmt(fat)}, speed {Fmt(speed)}, charm {Fmt(charm)}, skin color {FmtColor(skinColor)}, eye color {FmtColor(eyeColor)}");

        return new SharedPlayerData() {
            SkinColor = skinColor,
            EyeColor = eyeColor,
            EatsMeat = foodSleep > 5,
            Fat = fat,
            Speed = speed,
            Charm = charm,

            FoodMax = (byte)foodMax,
            FoodSleep = (byte)foodSleep,
            RunSpeed = 1 + speed * 0.12f - fat * 0.10f,
            PoleClimbSpeed = 1 + speed * 0.13f - fat * 0.10f,
            CorridorClimbSpeed = 1 + speed * 0.10f - fat * 0.10f,
            Weight = 1 + fat * 0.15f - charm * 0.12f,
            VisBonus = -charm * 0.15f,
            SneakStealth = 0.5f + charm * 0.15f - speed * 0.08f,
            Loudness = 1 + fat * 0.30f - charm * 0.15f,
            LungWeakness = 1 + charm * 0.18f,
            Ill = false,

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
            player.state = new PlayerState(player, pid, SlugcatStats.Name.White, false);

            AddPlayer(player);
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

    public override void AddPlayer(AbstractCreature player)
    {
        if (player.PlayerState().playerNumber < 0) {
            Main.Log.LogError($"Adding player with negative ID! {player}");
        }
        else if (playerSessionRecords.Length < player.PlayerState().playerNumber + 1) {
            Array.Resize(ref playerSessionRecords, player.PlayerState().playerNumber + 1);
        }
        base.AddPlayer(player);
    }

    public SharedPlayerData? GetPlayerData(int pid) => pid >= 0 && pid < Save.playerData.Count ? Save.playerData[pid].Shared : null;
    public SharedPlayerData? GetPlayerData(EntityID eid) => GetPlayerData(eid.number);
    public SharedPlayerData? GetPlayerData(AbstractCreature player) => GetPlayerData(player.ID.number);
    public SharedPlayerData? GetPlayerData(NetPeer peer) => peers.TryGetValue(peer.Id, out PeerData data) ? Save.playerData[data.Pid].Shared : null;

    public AbstractCreature? GetPlayer(NetPeer peer) => peers.TryGetValue(peer.Id, out PeerData data) ? Players[data.Pid] : null;
}
