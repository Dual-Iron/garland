using Common;

namespace Server;

partial class Main
{
    Timer rainUpdate = Timer.WithPeriod(15 * 40);
    Timer deathRainUpdate = Timer.WithPeriod(2 * 40);

    void SyncDeathRain(GlobalRain.DeathRain rain)
    {
        server.Broadcast(new SyncDeathRain((byte)rain.deathRainMode, rain.timeInThisMode, rain.progression, rain.calmBeforeStormSunlight));
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
        self.floodSpeed = 0.1f;

        float rainDirectionGetToLast = self.rainDirectionGetTo;

        orig(self);

        bool syncIfDirectionChange = self.Intensity > 0 || self.deathRain != null;
        bool directionChange = rainDirectionGetToLast != self.rainDirectionGetTo;

        if (syncIfDirectionChange && directionChange || ClientJustJoined || rainUpdate.Tick()) {
            rainUpdate.Reset();

            server.Broadcast(new SyncRain((ushort)self.game.world.rainCycle.timer, (ushort)self.game.world.rainCycle.cycleLength, self.rainDirection, self.rainDirectionGetTo));
        }
    }

    private void DeathRain_DeathRainUpdate(On.GlobalRain.DeathRain.orig_DeathRainUpdate orig, GlobalRain.DeathRain self)
    {
        orig(self);

        if (ClientJustJoined || deathRainUpdate.Tick()) {
            deathRainUpdate.Reset();

            SyncDeathRain(self);
        }
    }

    private void DeathRain_NextDeathRainMode(On.GlobalRain.DeathRain.orig_NextDeathRainMode orig, GlobalRain.DeathRain self)
    {
        var deathRainModeLast = self.deathRainMode;
        orig(self);
        if (deathRainModeLast != self.deathRainMode) {
            deathRainUpdate.Reset();

            SyncDeathRain(self);
        }
    }

    private void BrokenAntiGravity_Update(On.AntiGravity.BrokenAntiGravity.orig_Update orig, AntiGravity.BrokenAntiGravity self)
    {
        bool onLast = self.on;
        orig(self);
        if (onLast != self.on || ClientJustJoined) {
            server.Broadcast(new SyncAntiGrav(self.on, (ushort)self.counter, self.from, self.to));
        }
    }
}
