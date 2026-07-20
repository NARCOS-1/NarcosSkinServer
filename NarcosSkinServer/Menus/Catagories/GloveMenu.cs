using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosEconomy;
using NarcosSkinServer.Services;

namespace NarcosSkinServer.Menus.Catagories;

public static class GloveMenu
{
    public static void Open(
    CCSPlayerController player,
    BasePlugin plugin,
    EconomyService economyService,
    WasdMenu? previousMenu = null)
    {
        var gloves = Economy.GetGloves().ToList();

        var menu = new WasdMenu("Gloves", plugin)
        {
            PrevMenu = previousMenu
        };

        foreach (var glove in gloves)
        {
            menu.AddItem(glove.Name, (p, o) =>
            {
                GlovePaintKitMenu.Open(
                    p,
                    plugin,
                    economyService,
                    glove,
                    menu);
            });
        }

        menu.Display(player, 0);
    }
}
