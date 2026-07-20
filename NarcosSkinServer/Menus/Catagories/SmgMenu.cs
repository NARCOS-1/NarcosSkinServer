using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosSkinServer.Data;
using NarcosSkinServer.Models;
using NarcosSkinServer.Services;
using System.Linq;

namespace NarcosSkinServer.Menus.Catagories;

public static class SmgMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, EconomyService economyService, WasdMenu? previousMenu = null)
    {
        var menu = new WasdMenu("SMGs", plugin)
        {
            PrevMenu = previousMenu
        };

        foreach (var weapon in SkinCatalog.Weapons.Values.Where(w => w.Category == WeaponCategory.SMG))
        {
            menu.AddItem(weapon.Name, (p, o) =>
            {
                PaintKitMenu.Open(p, plugin, economyService, weapon, menu);
            });
        }

        menu.Display(player, 0);
    }
}
