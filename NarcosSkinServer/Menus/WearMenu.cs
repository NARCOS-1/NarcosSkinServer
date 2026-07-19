using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosSkinServer.Data;
using NarcosSkinServer.Models;
using NarcosSkinServer.Services;
using NarcosEconomy.Models;

namespace NarcosSkinServer.Menus;

public static class WearMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, EconomyService economyService, WeaponDefinition weapon, int paintKit)
    {
        var menu = new WasdMenu($"{weapon.Name} | Wear", plugin);

        foreach (var preset in WearPresets.All)
        {
            AddWear(
            menu,
            plugin,
            economyService,
            weapon,
            paintKit,
            preset.Name,
            preset.Wear);
        }

        menu.Display(player, 0);
    }

    private static void AddWear(
     WasdMenu menu,
     BasePlugin plugin,
     EconomyService economyService,
     WeaponDefinition weapon,
     int paintKit,
     string name,
     float wear)
    {
        menu.AddItem(name, (player, option) =>
        {
            SeedMenu.Open(player, plugin, economyService, weapon, paintKit, wear);
        });
    }

    public static void Open(
    CCSPlayerController player,
    BasePlugin plugin,
    EconomyService economyService,
    Glove glove,
    int paintKit)
    {
        var menu = new WasdMenu($"{glove.Name} | Wear", plugin);

        foreach (var preset in WearPresets.All)
        {
            AddWear(
                menu,
                plugin,
                economyService,
                glove,
                paintKit,
                preset.Name,
                preset.Wear);
        }

        menu.Display(player, 0);
    }

    private static void AddWear(
        WasdMenu menu,
        BasePlugin plugin,
        EconomyService economyService,
        Glove glove,
        int paintKit,
        string name,
        float wear)
    {
        menu.AddItem(name, (player, option) =>
        {
            GloveSeedMenu.Open(
                player,
                plugin,
                economyService,
                glove,
                paintKit,
                wear);
        });
    }
}