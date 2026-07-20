using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosSkinServer.Data;
using NarcosEconomy.Models;
using NarcosSkinServer.Services;

namespace NarcosSkinServer.Menus;

public static class GloveSeedMenu
{
    public static void Open(
        CCSPlayerController player,
        BasePlugin plugin,
        EconomyService economyService,
        Glove glove,
        int paintKit,
        float wear,
        WasdMenu? previousMenu = null)
    {
        var menu = new WasdMenu($"{glove.Name} | Seed", plugin)
        {
            PrevMenu = previousMenu
        };

        AddSeed(menu, plugin, economyService, glove, paintKit, wear, 0);
        AddSeed(menu, plugin, economyService, glove, paintKit, wear, 1);
        AddSeed(menu, plugin, economyService, glove, paintKit, wear, 69);
        AddSeed(menu, plugin, economyService, glove, paintKit, wear, 420);
        AddSeed(menu, plugin, economyService, glove, paintKit, wear, 661);
        AddSeed(menu, plugin, economyService, glove, paintKit, wear, 1000);

        menu.AddItem("Custom Seed...", (p, o) =>
        {
            Variables.PendingGloveSeedInput[p.Slot] = (glove, paintKit, wear);
            p.PrintToChat("[Narcos] Type the seed number (0-1000000) in chat.");
        });

        menu.Display(player, 0);
    }

    private static void AddSeed(
        WasdMenu menu,
        BasePlugin plugin,
        EconomyService economyService,
        Glove glove,
        int paintKit,
        float wear,
        int seed)
    {
        menu.AddItem(seed.ToString(), (player, option) =>
        {
            economyService.ApplyGlove(
                player,
                glove.DefIndex,
                paintKit,
                wear,
                seed);
        });
    }
}
