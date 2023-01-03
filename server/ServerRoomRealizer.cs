using Common;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server;

sealed class ServerRoomRealizer
{
    struct PeerState
    {
        public AbstractCreature player;
        public NetPeer netPeer;
        public List<AbstractRoom> realized;
    }

    static readonly RoomRealizer fake = new(null, null);
    static float PerformanceEstimate(AbstractRoom room, float budget = 1500f)
    {
        fake.performanceBudget = budget;
        return fake.RoomPerformanceEstimation(room);
    }

    readonly RainWorldGame game;
    readonly ServerSession session;
    readonly List<PeerState> tracked = new();
    readonly List<AbstractRoom> serverRealized = new();

    // TODO: re-abstractizing rooms lol

    public ServerRoomRealizer(RainWorldGame game, ServerSession session)
    {
        this.game = game;
        this.session = session;
    }

    void Realize(PeerState peer, AbstractRoom room)
    {
        if (!peer.realized.Contains(room)) {
            Main.Log.LogDebug($"Requesting player {peer.player.ID.number} to realize {room.name}");

            peer.realized.Add(room);

            // Tell client to realize the room if it hasn't already
            peer.netPeer.Send(new RealizeRoom(room.index));

            // Start realizing the room on the server
            if (!serverRealized.Contains(room)) {
                serverRealized.Add(room);
                room.world.ActivateRoom(room);
            }
        }
    }

    public void Update()
    {
        UpdatePeerList();

        foreach (var player in tracked) {
            UpdatePeer(player);
        }
    }

    private void UpdatePeerList()
    {
        // Look for new untracked peers
        foreach (var peer in Main.Instance.server.ConnectedPeerList) {
            var player = session.GetPlayer(peer);
            if (player == null || tracked.Any(p => p.player.ID == player.ID)) {
                continue;
            }

            tracked.Add(new() {
                player = player,
                netPeer = peer,
                realized = new()
            });
        }
        // Remove old peers that have disconnected
        for (int i = tracked.Count - 1; i >= 0; i--) {
            if (tracked[i].netPeer.ConnectionState == ConnectionState.Disconnected) {
                tracked.RemoveAt(i);
            }
        }
    }

    private void UpdatePeer(PeerState peer)
    {
        Realize(peer, peer.player.Room);

        foreach (var connection in peer.player.Room.connections) {
            if (connection > -1) {
                Realize(peer, game.world.GetAbstractRoom(connection));
            }
        }
    }
}
