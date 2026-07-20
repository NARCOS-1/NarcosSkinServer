using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosEconomy;
using NarcosSkinServer.Data;
using NarcosSkinServer.Menus;
using NarcosSkinServer.Models;
using NarcosSkinServer.Services;

namespace NarcosSkinServer.Menus.Catagories;

public static class PaintKitMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, EconomyService economyService, WeaponDefinition weapon, WasdMenu? previousMenu = null)
    {
        var menu = new WasdMenu(weapon.Name, plugin)
        {
            PrevMenu = previousMenu
        };

        var econWeapon = Economy.GetWeapon(weapon.DefIndex);

        if (econWeapon == null)
            return;

        foreach (var paintKit in econWeapon.PaintKits)
        {

            menu.AddItem(paintKit.Name, (p, o) =>
            {
                WearMenu.Open(
                    p,
                    plugin,
                    economyService,
                    weapon,
                    paintKit.Id,
                    menu);
            });
        }


        menu.Display(player, 0);
    }
}
