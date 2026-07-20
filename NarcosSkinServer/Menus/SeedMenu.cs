using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosSkinServer.Data;
using NarcosSkinServer.Models;
using NarcosSkinServer.Services;

namespace NarcosSkinServer.Menus;

public static class SeedMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, EconomyService economyService, WeaponDefinition weapon, int paintKit, float wear, WasdMenu? previousMenu = null)
    {
        var menu = new WasdMenu($"{weapon.Name} | Seed", plugin)
        {
            PrevMenu = previousMenu
        };

        AddSeed(menu, economyService, weapon, paintKit, wear, "Default (0)", 0);
        AddSeed(menu, economyService, weapon, paintKit, wear, "100", 100);
        AddSeed(menu, economyService, weapon, paintKit, wear, "250", 250);
        AddSeed(menu, economyService, weapon, paintKit, wear, "500", 500);
        AddSeed(menu, economyService, weapon, paintKit, wear, "661", 661);

        menu.AddItem("Random", (p, o) =>
        {
            int seed = Random.Shared.Next(1, 1001);

            economyService.ApplySkin(
                p,
                weapon,
                paintKit,
                wear,
                seed);
        });

        menu.AddItem("Custom Seed...", (p, o) =>
        {
            Variables.PendingWeaponSeedInput[p.Slot] = (weapon, paintKit, wear);
            p.PrintToChat("[Narcos] Type the seed number (0-1000000) in chat.");
        });

        menu.Display(player, 0);
    }

    private static void AddSeed(
        WasdMenu menu,
        EconomyService economyService,
        WeaponDefinition weapon,
        int paintKit,
        float wear,
        string name,
        int seed)
    {
        menu.AddItem(name, (player, option) =>
        {
            economyService.ApplySkin(
                player,
                weapon,
                paintKit,
                wear,
                seed);
        });
    }
}
