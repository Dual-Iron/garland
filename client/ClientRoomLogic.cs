using Common;

namespace Client;

sealed class ClientRoomLogic
{
    readonly RainWorldGame game;
    readonly ClientSession session;

    public ClientRoomLogic(RainWorldGame game, ClientSession session)
    {
        this.game = game;
        this.session = session;
    }

    public void Update()
    {
        // Follow our own player
        game.cameras[0].followAbstractCreature = session.MyPlayer;

        // Read ALL RealizeRoom packets, not just the latest one!!
        while (RealizeRoom.Queue.Dequeue(out _, out var packet)) {
            Main.Log.LogDebug($"Respecting request to realize {game.world.GetAbstractRoom(packet.Index).name}");

            // Logic is dead simple though.
            game.world.ActivateRoom(packet.Index);
        }
    }
}
