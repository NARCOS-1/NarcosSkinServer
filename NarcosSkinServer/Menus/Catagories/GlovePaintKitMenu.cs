using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosEconomy;
using NarcosEconomy.Models;
using NarcosSkinServer.Services;

namespace NarcosSkinServer.Menus.Catagories;

public static class GlovePaintKitMenu
{
    public static void Open(
        CCSPlayerController player,
        BasePlugin plugin,
        EconomyService economyService,
        Glove glove)
    {
        var menu = new WasdMenu(glove.Name, plugin);

        foreach (var paintKit in glove.PaintKits)
        {
            menu.AddItem(paintKit.Name, (p, o) =>
            {
                WearMenu.Open(
                    p,
                    plugin,
                    economyService,
                    glove,
                    paintKit.Id);
            });
        }

        menu.Display(player, 0);
    }
}