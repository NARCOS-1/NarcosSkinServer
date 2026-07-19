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
    EconomyService economyService)
    {
        var gloves = Economy.GetGloves().ToList();

        player.PrintToChat($"Gloves loaded: {gloves.Count}");

        var menu = new WasdMenu("Gloves", plugin);

        foreach (var glove in gloves)
        {
            menu.AddItem(glove.Name, (p, o) =>
            {
                GlovePaintKitMenu.Open(
                    p,
                    plugin,
                    economyService,
                    glove);
            });
        }

        player.PrintToChat($"Menu items: {gloves.Count}");

        menu.Display(player, 0);
    }
}