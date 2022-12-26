using Menu;
using UnityEngine;

namespace Garland.Menus;

sealed class OnlineMenu : Menu.Menu
{
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

        pages[0].subObjects.Add(new SimpleButton(this, pages[0], Translate("BACK"), "BACK", new Vector2(200f, 50f), new Vector2(110f, 30f)));
    }

    public override void Singal(MenuObject sender, string message)
    {
        if (message == "BACK") {
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            PlaySound(SoundID.MENU_Switch_Page_Out);
        }
    }
}
