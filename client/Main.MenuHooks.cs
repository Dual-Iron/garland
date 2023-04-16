using Menu;
using System.Linq;

namespace Client;

partial class Main
{
    const string signal = "GARLAND_ONLINE";

    private void MenuHooks()
    {
        On.Menu.MainMenu.ctor += ReplaceSingleplayerButton;
        On.Menu.MainMenu.Singal += MainMenu_Singal;
        On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;
    }

    private void ReplaceSingleplayerButton(On.Menu.MainMenu.orig_ctor orig, Menu.MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
    {
        orig(self, manager, showRegionSpecificBkg);

        MenuObject? add = null;

        foreach (var button in self.pages[0].subObjects.OfType<SimpleButton>()) {
            // Add MULTI PLAYER button at location of REGIONS button
            if (add == null && button.signalText == "REGIONS") {
                add = new SimpleButton(self, self.pages[0], "MULTI PLAYER", signal, button.pos, button.size);
                add.nextSelectable[0] = add;
                add.nextSelectable[2] = add;
            }

            // Move other buttons down
            if (add != null) {
                button.pos.y -= 40;
            }
        }

        if (add != null)
            self.pages[0].subObjects.Add(add);
        else
            Log.LogError("MULTI PLAYER button not added to main menu!");
    }

    private void MainMenu_Singal(On.Menu.MainMenu.orig_Singal orig, MainMenu self, MenuObject sender, string message)
    {
        if (message == signal) {
            self.manager.RequestMainProcessSwitch(Enums.OnlineMenu);
            self.PlaySound(SoundID.MENU_Switch_Page_In);
        }
        else {
            orig(self, sender, message);
        }
    }

    private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
    {
        if (ID == Enums.OnlineMenu) {
            self.currentMainLoop = new Menus.OnlineMenu(self);
        }
        orig(self, ID);
    }
}
