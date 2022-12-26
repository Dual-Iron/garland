using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Linq;
using UnityEngine;

namespace Garland;

sealed partial class Plugin
{
    const string signal = "GARLAND_ONLINE";
    const ProcessManager.ProcessID onlineMenu = (ProcessManager.ProcessID)(-10933);

    private static void MenuHooks()
    {
        On.Menu.MainMenu.ctor += AddMultiplayerButtonToMainMenu;
        On.Menu.MainMenu.Singal += MainMenu_Singal;
        IL.ProcessManager.SwitchMainProcess += ProcessManager_SwitchMainProcess;
    }

    private static void AddMultiplayerButtonToMainMenu(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
        orig(self, manager, showRegionSpecificBkg);

        foreach (var button in self.pages[0].subObjects.OfType<SimpleButton>()) {
            var oldPos = button.pos;

            button.pos.y += 40;

            if (button.signalText == "SINGLE PLAYER") {
                var onlineButton = new SimpleButton(self, self.pages[0], "MULTI PLAYER", signal, oldPos, button.size);
                onlineButton.nextSelectable[0] = onlineButton;
                onlineButton.nextSelectable[2] = onlineButton;
                self.pages[0].subObjects.Add(onlineButton);
                return;
            }
        }

        Logger.LogError("MULTI PLAYER button not added to main menu because SINGLE PLAYER button was missing!");
    }

    private static void MainMenu_Singal(On.Menu.MainMenu.orig_Singal orig, MainMenu self, MenuObject sender, string message)
    {
        if (message == signal) {
            self.manager.RequestMainProcessSwitch(onlineMenu);
            self.PlaySound(SoundID.MENU_Switch_Page_In);
        }
        else {
            orig(self, sender, message);
        }
    }

    private static void ProcessManager_SwitchMainProcess(ILContext il)
    {
        ILCursor cursor = new(il);

        cursor.GotoNext(i => i.MatchSwitch(out _));

        // TrySwitchToModMenu(this, ID);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate(SwitchToCustomProcess);
    }

    private static void SwitchToCustomProcess(ProcessManager pm, ProcessManager.ProcessID pid)
    {
        if (pid == onlineMenu) {
            pm.currentMainLoop = new Menus.OnlineMenu(pm, onlineMenu);
        }
    }
}
