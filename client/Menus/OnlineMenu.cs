using Common;
using Menu;
using System.Diagnostics;
using UnityEngine;

namespace Client.Menus;

sealed class OnlineMenu : Menu.Menu
{
    const int port = Variables.DefaultPort;

    readonly SimpleButton back;
    readonly SimpleButton join;

    public OnlineMenu(ProcessManager manager, ProcessManager.ProcessID ID) : base(manager, ID)
    {
        pages.Add(new(this, null, "main", 0));

        // Big pretty background picture
        pages[0].subObjects.Add(new InteractiveMenuScene(this, pages[0], MenuScene.SceneID.Intro_5_Hunting) { cameraRange = 0.25f });

        // A scaled up translucent black pixel to make the background less distracting
        pages[0].subObjects.Add(new MenuSprite(pages[0], new(-1, -1), new("pixel") {
            color = new(0, 0, 0, 0.75f),
            scaleX = 2000,
            scaleY = 1000,
            anchorX = 0,
            anchorY = 0
        }));

        pages[0].subObjects.Add(back = new SimpleButton(this, pages[0], Translate("BACK"), "", new Vector2(200, 50), new Vector2(110, 30)));
        pages[0].subObjects.Add(join = new SimpleButton(this, pages[0], "JOIN", "", new Vector2(1056, 50), new Vector2(110, 30)));
    }

    public override void Singal(MenuObject sender, string message)
    {
        if (sender == back) {
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            PlaySound(SoundID.MENU_Switch_Page_Out);
        }
        else if (sender == join) {
            Plugin.StartConnecting("localhost", port);
            PlaySound(SoundID.MENU_Start_New_Game);
        }
    }

    public override void Update()
    {
        base.Update();

        bool greyed = Plugin.ClientState != ConnectionState.Disconnected;

        back.GetButtonBehavior.greyedOut = greyed;
        join.GetButtonBehavior.greyedOut = greyed;

        if (manager.upcomingProcess == null && Plugin.ClientState == ConnectionState.Connected && Plugin.StartRoom != null) {
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
            manager.menuSetup.startGameCondition = Plugin.online;
        }
    }
}
