using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;

namespace NarcosSkinServer.Menus.Knives;

public static class ButterflyKnifeMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin)
    {
        var menu = new WasdMenu("Butterfly Knife", plugin);

        menu.AddItem("Doppler", (p, o) =>
        {
            p.PrintToChat("Doppler");
        });

        menu.AddItem("Fade", (p, o) =>
        {
            p.PrintToChat("Fade");
        });

        menu.AddItem("Marble Fade", (p, o) =>
        {
            p.PrintToChat("Marble Fade");
        });

        menu.AddItem("Tiger Tooth", (p, o) =>
        {
            p.PrintToChat("Tiger Tooth");
        });

        menu.AddItem("Crimson Web", (p, o) =>
        {
            p.PrintToChat("Crimson Web");
        });

        menu.Display(player, 0);
    }
}