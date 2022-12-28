using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Linq;
using UnityEngine;

namespace Client;

sealed partial class Plugin
{
    const string signal = "GARLAND_ONLINE";
    const ProcessManager.ProcessID onlineMenu = (ProcessManager.ProcessID)(-10933);

    private static void MenuHooks()
    {
        On.Menu.MainMenu.ctor += ReplaceSingleplayerButton;
        On.Menu.MainMenu.Singal += MainMenu_Singal;
        IL.ProcessManager.SwitchMainProcess += ProcessManager_SwitchMainProcess;
    }

    private static void ReplaceSingleplayerButton(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
        orig(self, manager, showRegionSpecificBkg);

        foreach (var button in self.pages[0].subObjects.OfType<SimpleButton>()) {
            if (button.signalText == "SINGLE PLAYER") {
                button.signalText = signal;
                button.menuLabel.text = "MULTI PLAYER";
                return;
            }
        }

        Log.LogError("Where is the singleplayer button???");
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
