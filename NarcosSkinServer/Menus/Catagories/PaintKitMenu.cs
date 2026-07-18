using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosSkinServer.Data;
using NarcosSkinServer.Menus;
using NarcosSkinServer.Models;
using NarcosSkinServer.Services;

namespace NarcosSkinServer.Menus.Catagories;

public static class PaintKitMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, EconomyService economyService, WeaponDefinition weapon)
    {
        

        var menu = new WasdMenu(weapon.Name, plugin);


        foreach (var paintKit in PaintKitLoader.GetForWeapon(weapon.DefIndex))
        {
            menu.AddItem(paintKit.Name, (p, o) =>
            {
                WearMenu.Open(
                    p,
                    plugin,
                    economyService,
                    weapon,
                    paintKit.Id);
            });
        }
        

        menu.Display(player, 0);
    }
}