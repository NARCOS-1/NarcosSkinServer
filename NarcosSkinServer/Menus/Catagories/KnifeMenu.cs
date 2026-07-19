using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosSkinServer.Data;
using NarcosSkinServer.Menus.Knives;
using NarcosSkinServer.Models;
using NarcosSkinServer.Services;
using System.Linq;


namespace NarcosSkinServer.Menus.Catagories;

public static class KnifeMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, EconomyService economyService)
    {
        var menu = new WasdMenu("Knives", plugin);

        foreach (var weapon in SkinCatalog.Weapons.Values.Where(w => w.Category == WeaponCategory.Knife))
        {
            menu.AddItem(weapon.Name, (p, o) =>
            {
                PaintKitMenu.Open(p, plugin, economyService, weapon);
            });
        }

        menu.Display(player, 0);
    }
}