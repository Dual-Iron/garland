using Common;
using HUD;
using System;

namespace Client;

partial class Main
{
    private void GameHooks()
    {
        On.GlobalRain.Update += GlobalRain_Update;
    }

    private void GlobalRain_Update(On.GlobalRain.orig_Update orig, GlobalRain self)
    {
        orig(self);

        if (self.game.session is not ClientSession session) return;

        RainCycle rainCycle = self.game.world.rainCycle;

        if (SyncRain.Latest(out var syncRain)) {
            syncRain.Deconstruct(out var timer, out var cycleLength, out self.rainDirection, out self.rainDirectionGetTo);

            rainCycle.timer = timer;
            rainCycle.cycleLength = cycleLength;

            if (self.game.cameras[0].hud?.rainMeter is RainMeter rm && rm.circles.Length != Math.Min(30, rainCycle.cycleLength / 1200)) {
                FixCircles(rm, session);
            }
        }

        if (SyncDeathRain.Latest(out var deathRain)) {
            if (!rainCycle.deathRainHasHit) {
                rainCycle.deathRainHasHit = true;
                rainCycle.RainHit();
            }

            deathRain.Deconstruct(out var mode, out self.deathRain!.timeInThisMode, out self.deathRain.progression, out self.deathRain.calmBeforeStormSunlight, out self.flood, out self.floodSpeed);

            self.deathRain.deathRainMode = (GlobalRain.DeathRain.DeathRainMode)mode;

            CatchUpDeathRain(self, self.deathRain.deathRainMode);
        }

        if (SyncAntiGrav.Latest(out var antiGrav)) {
            rainCycle.brokenAntiGrav ??= new(self.game.setupValues.gravityFlickerCycleMin, self.game.setupValues.gravityFlickerCycleMax, self.game);

            antiGrav.Deconstruct(out rainCycle.brokenAntiGrav.on, out var counter, out rainCycle.brokenAntiGrav.from, out rainCycle.brokenAntiGrav.to);

            rainCycle.brokenAntiGrav.progress = 0;
            rainCycle.brokenAntiGrav.counter = counter;
        }
    }

    private void FixCircles(RainMeter rm, ClientSession session)
    {
        FContainer container = rm.circles[0].sprite.container;

        // Remove old circles
        foreach (var circle in rm.circles) {
            circle.sprite.RemoveFromContainer();
        }

        // Add the new ones :)
        rm.circles = new HUDCircle[Math.Min(30, session.game.world.rainCycle.cycleLength / 1200)];
        for (int i = 0; i < rm.circles.Length; i++) {
            rm.circles[i] = new HUDCircle(rm.hud, HUDCircle.SnapToGraphic.smallEmptyCircle, container, 0);
        }
    }

    private void CatchUpDeathRain(GlobalRain rain, GlobalRain.DeathRain.DeathRainMode mode)
    {
        // GradeAPlateu, GradeBPlateu, and Mayhem do not have special update logic, and inherit previous stages
        
        // This basically sets values to what they *would* be, if they had progressed past the previous stage (progression = 1).
        // See GlobalRain::DeathRain::Update

        switch (mode) {
            case GlobalRain.DeathRain.DeathRainMode.GradeAPlateu:
                rain.Intensity = 0.6f;
                rain.MicroScreenShake = 1.5f;
                rain.bulletRainDensity = 0f;
                break;

            case GlobalRain.DeathRain.DeathRainMode.GradeBPlateu:
                rain.Intensity = 0.71f;
                rain.MicroScreenShake = 2.1f;
                rain.ScreenShake = 1.2f;
                break;

            case GlobalRain.DeathRain.DeathRainMode.Mayhem:
                rain.Intensity = 1f;
                rain.MicroScreenShake = 4f;
                rain.ScreenShake = 3f;
                break;
        }

        // Also this
        if (mode > GlobalRain.DeathRain.DeathRainMode.CalmBeforeStorm) {
            rain.ShaderLight = -1f;
        }
    }
}
