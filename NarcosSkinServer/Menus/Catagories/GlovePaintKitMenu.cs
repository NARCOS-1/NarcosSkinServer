using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosEconomy;
using NarcosEconomy.Models;
using NarcosSkinServer.Services;
using System.Linq;

namespace NarcosSkinServer.Menus.Catagories;

public static class GlovePaintKitMenu
{
    public static void Open(
        CCSPlayerController player,
        BasePlugin plugin,
        EconomyService economyService,
        Glove glove,
        WasdMenu? previousMenu = null)
    {
        var menu = new WasdMenu(glove.Name, plugin)
        {
            PrevMenu = previousMenu
        };

        foreach (var paintKit in glove.PaintKits.OrderBy(pk => pk.Name, StringComparer.OrdinalIgnoreCase))
        {
            menu.AddItem(paintKit.Name, (p, o) =>
            {
                WearMenu.Open(
                    p,
                    plugin,
                    economyService,
                    glove,
                    paintKit.Id,
                    menu);
            });
        }

        menu.Display(player, 0);
    }
}
