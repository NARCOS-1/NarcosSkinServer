using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;

namespace NarcosSkinServer.Menus.Catagories;

public static class WeaponCategoryMenu
{
    public static void OpenKnives(CCSPlayerController player, BasePlugin plugin)
    {
        var menu = new WasdMenu("Knives", plugin);

        menu.AddItem("Butterfly Knife", (p, o) =>
        {
            p.PrintToChat("Butterfly");
        });

        menu.AddItem("Karambit", (p, o) =>
        {
            p.PrintToChat("Karambit");
        });

        menu.AddItem("M9 Bayonet", (p, o) =>
        {
            p.PrintToChat("M9");
        });

        menu.AddItem("Skeleton Knife", (p, o) =>
        {
            p.PrintToChat("Skeleton");
        });
        player.PrintToChat("[Narcos] Display");
        menu.Display(player, 0);
    }
}