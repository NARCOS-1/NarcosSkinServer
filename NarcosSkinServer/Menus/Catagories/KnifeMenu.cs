using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosSkinServer.Data;
using NarcosSkinServer.Menus.Knives;
using NarcosSkinServer.Services;


namespace NarcosSkinServer.Menus.Catagories;

public static class KnifeMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, EconomyService economyService)
    {
        var menu = new WasdMenu("Knives", plugin);

        foreach (var weapon in SkinCatalog.Weapons.Values)
        {
            menu.AddItem(weapon.Name, (p, o) =>
            {
                PaintKitMenu.Open(p, plugin, economyService, weapon);
            });
        }

        menu.Display(player, 0);
    }
}