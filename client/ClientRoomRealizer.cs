using Common;

namespace Client;

sealed class ClientRoomRealizer
{
    readonly RainWorldGame game;
    readonly ClientSession session;

    public ClientRoomRealizer(RainWorldGame game, ClientSession session)
    {
        this.game = game;
        this.session = session;
    }

    public void Update()
    {
        // Follow own player. This isn't really room realizing logic, I just need to put this somewhere. Might consider renaming this class to "ClientRoomLogic".
        game.cameras[0].followAbstractCreature = session.MyPlayer;

        // Read ALL RealizeRoom packets, not just the latest one!!
        while (RealizeRoom.Queue.Dequeue(out _, out var packet)) {
            Main.Log.LogDebug($"Respecting request to realize {game.world.GetAbstractRoom(packet.Index).name}");

            // Logic is dead simple though.
            game.world.ActivateRoom(packet.Index);
        }
    }
}