using Common;
using HUD;
using System;

namespace Client;

partial class Main
{
    private void GameHooks()
    {
        On.HUD.RainMeter.ctor += UpdateCircles;
        On.GlobalRain.Update += SyncRain;
    }

    private void UpdateCircles(On.HUD.RainMeter.orig_ctor orig, RainMeter self, HUD.HUD hud, FContainer fContainer)
    {
        const int maxCircleCount = 30;

        var cyc = (hud.owner as Player)!.room.world.rainCycle;
        var len = cyc.cycleLength;
        if (cyc.cycleLength > maxCircleCount * 1200) {
            cyc.cycleLength = maxCircleCount * 1200;
        }
        try { orig(self, hud, fContainer); }
        finally { cyc.cycleLength = len; }
    }

    private void SyncRain(On.GlobalRain.orig_Update orig, GlobalRain self)
    {
        orig(self);

        if (self.game.session is not ClientSession) return;

        RainCycle rainCycle = self.game.world.rainCycle;

        if (Common.SyncRain.Latest(out var syncRain)) {
            syncRain.Deconstruct(out var timer, out var cycleLength, out self.rainDirection, out self.rainDirectionGetTo);

            rainCycle.timer = timer;
            rainCycle.cycleLength = cycleLength;
        }

        if (SyncDeathRain.Latest(out var deathRain)) {
            if (!rainCycle.deathRainHasHit) {
                rainCycle.deathRainHasHit = true;
                rainCycle.RainHit();
            }

            deathRain.Deconstruct(out self.deathRain!.timeInThisMode, out self.deathRain.progression, out self.deathRain.calmBeforeStormSunlight, out self.flood, out self.floodSpeed, out string mode);

            self.deathRain.deathRainMode = new(mode);

            CatchUpDeathRain(self, self.deathRain.deathRainMode);
        }

        if (SyncAntiGrav.Latest(out var antiGrav)) {
            rainCycle.brokenAntiGrav ??= new(self.game.setupValues.gravityFlickerCycleMin, self.game.setupValues.gravityFlickerCycleMax, self.game);

            antiGrav.Deconstruct(out rainCycle.brokenAntiGrav.on, out var counter, out rainCycle.brokenAntiGrav.from, out rainCycle.brokenAntiGrav.to);

            rainCycle.brokenAntiGrav.progress = 0;
            rainCycle.brokenAntiGrav.counter = counter;
        }
    }

    private void CatchUpDeathRain(GlobalRain rain, GlobalRain.DeathRain.DeathRainMode mode)
    {
        // GradeAPlateu, GradeBPlateu, and Mayhem do not have special update logic, and inherit previous stages
        
        // This basically sets values to what they *would* be, if they had progressed past the previous stage (progression = 1).
        // See GlobalRain::DeathRain::Update

        if (mode == GlobalRain.DeathRain.DeathRainMode.GradeAPlateu) {
            rain.Intensity = 0.6f;
            rain.MicroScreenShake = 1.5f;
            rain.bulletRainDensity = 0f;
        }
        else if (mode == GlobalRain.DeathRain.DeathRainMode.GradeBPlateu) {
            rain.Intensity = 0.71f;
            rain.MicroScreenShake = 2.1f;
            rain.ScreenShake = 1.2f;
        }
        else if (mode == GlobalRain.DeathRain.DeathRainMode.Mayhem) {
            rain.Intensity = 1f;
            rain.MicroScreenShake = 4f;
            rain.ScreenShake = 3f;
        }

        // Also this
        if (mode != GlobalRain.DeathRain.DeathRainMode.None && mode != GlobalRain.DeathRain.DeathRainMode.CalmBeforeStorm) {
            rain.ShaderLight = -1f;
        }
    }
}
