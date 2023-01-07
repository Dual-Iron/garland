using Common;
using LiteNetLib;

namespace Server;

partial class Main
{
    Timer rainUpdate = Timer.WithPeriod(15 * 40);
    Timer deathRainUpdate = Timer.WithPeriod(2 * 40);

    public void CatchUp(NetPeer peer, RainWorldGame game)
    {
        peer.Send(new SyncRain(game.world.rainCycle.timer, game.world.rainCycle.cycleLength, game.globalRain.rainDirection, game.globalRain.rainDirectionGetTo));
        if (game.globalRain.deathRain != null) {
            peer.Send(ToPacket(game.globalRain.deathRain));
        }
        if (game.world.rainCycle.brokenAntiGrav is AntiGravity.BrokenAntiGravity broken) {
            peer.Send(new SyncAntiGrav(broken.on, (ushort)broken.counter, broken.from, broken.to));
        }
    }

    private SyncDeathRain ToPacket(GlobalRain.DeathRain rain)
    {
        return new SyncDeathRain((byte)rain.deathRainMode, rain.timeInThisMode, rain.progression, rain.calmBeforeStormSunlight, rain.globalRain.flood, rain.globalRain.floodSpeed);
    }

    private void GameHooks()
    {
        On.GlobalRain.Update += GlobalRain_Update;
        On.GlobalRain.DeathRain.DeathRainUpdate += DeathRain_DeathRainUpdate;
        On.GlobalRain.DeathRain.NextDeathRainMode += DeathRain_NextDeathRainMode;
        On.AntiGravity.BrokenAntiGravity.Update += BrokenAntiGravity_Update;
    }

    private void GlobalRain_Update(On.GlobalRain.orig_Update orig, GlobalRain self)
    {
        float rainDirectionGetToLast = self.rainDirectionGetTo;

        orig(self);

        bool syncIfDirectionChange = self.Intensity > 0 || self.deathRain != null;
        bool directionChange = rainDirectionGetToLast != self.rainDirectionGetTo;

        if (syncIfDirectionChange && directionChange || rainUpdate.Tick()) {
            rainUpdate.Reset();

            server.Broadcast(new SyncRain(self.game.world.rainCycle.timer, self.game.world.rainCycle.cycleLength, self.rainDirection, self.rainDirectionGetTo));
        }
    }

    private void DeathRain_DeathRainUpdate(On.GlobalRain.DeathRain.orig_DeathRainUpdate orig, GlobalRain.DeathRain self)
    {
        orig(self);

        if (deathRainUpdate.Tick()) {
            deathRainUpdate.Reset();

            server.Broadcast(ToPacket(self));
        }
    }

    private void DeathRain_NextDeathRainMode(On.GlobalRain.DeathRain.orig_NextDeathRainMode orig, GlobalRain.DeathRain self)
    {
        var deathRainModeLast = self.deathRainMode;
        orig(self);
        if (deathRainModeLast != self.deathRainMode) {
            deathRainUpdate.Reset();

            server.Broadcast(ToPacket(self));
        }
    }

    private void BrokenAntiGravity_Update(On.AntiGravity.BrokenAntiGravity.orig_Update orig, AntiGravity.BrokenAntiGravity self)
    {
        bool onLast = self.on;
        orig(self);
        if (onLast != self.on) {
            server.Broadcast(new SyncAntiGrav(self.on, (ushort)self.counter, self.from, self.to));
        }
    }
}
