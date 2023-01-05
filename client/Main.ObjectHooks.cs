using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client;

partial class Main
{
    private void ObjectHooks()
    {
        On.AbstractPhysicalObject.Destroy += AbstractPhysicalObject_Destroy;
        On.AbstractPhysicalObject.ctor += AbstractPhysicalObject_ctor;

        // Send input to server
        On.Player.checkInput += Player_checkInput;

        // Set player input according to last package received by server
        On.RWInput.PlayerInput += RWInput_PlayerInput;
    }

    private void AbstractPhysicalObject_Destroy(On.AbstractPhysicalObject.orig_Destroy orig, AbstractPhysicalObject self)
    {
        orig(self);
        if (self.world.game.session is ClientSession session) {
            session.Objects.Remove(self.ID.number);
        }
    }

    private void AbstractPhysicalObject_ctor(On.AbstractPhysicalObject.orig_ctor orig, AbstractPhysicalObject self, World world, AbstractPhysicalObject.AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID)
    {
        orig(self, world, type, realizedObject, pos, ID);

        self.destroyOnAbstraction = true;
    }

    Input? fake;
    private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        fake = null;
        if (self.Game().session is ClientSession session) {
            if (self.playerState.playerNumber == session.ClientPid) {
                Player.InputPackage authentic = RWInput.PlayerInput(self.playerState.playerNumber, self.room.game.rainWorld.options, self.room.game.setupValues);

                ServerPeer?.Send(authentic.ToPacket(), LiteNetLib.DeliveryMethod.ReliableSequenced);
            }
            else {
                fake = session.LastInput[self.abstractPhysicalObject.ID.number];
            }
        }
        orig(self);
    }
    private Player.InputPackage RWInput_PlayerInput(On.RWInput.orig_PlayerInput orig, int playerNumber, Options options, RainWorldGame.SetupValues setup)
    {
        if (fake != null) {
            return fake.Value.ToPackage();
        }
        return orig(playerNumber, options, setup);
    }
}
