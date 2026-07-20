using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosSkinServer.Data;
using NarcosSkinServer.Services;

namespace NarcosSkinServer.Menus.Catagories;

public class MainMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, EconomyService economyService)
    {
        var menu = new WasdMenu("Narcos Skin Server", plugin);

        menu.AddItem("Knives", (p, option) =>
        {
            KnifeMenu.Open(player, plugin, economyService, menu);
        });

        menu.AddItem("Gloves", (p, option) =>
        {
            GloveMenu.Open(p, plugin, economyService, menu);
        });

        menu.AddItem("Pistols", (p, option) =>
        {
            PistolMenu.Open(p, plugin, economyService, menu);
        });

        menu.AddItem("SMGs", (p, option) =>
        {
            SmgMenu.Open(p, plugin, economyService, menu);
        });

        menu.AddItem("Rifles", (p, option) =>
        {
            RifleMenu.Open(p, plugin, economyService, menu);
        });

        menu.AddItem("Snipers", (p, option) =>
        {
            SniperMenu.Open(p, plugin, economyService, menu);
        });

        menu.AddItem("Heavy", (p, option) =>
        {
            HeavyMenu.Open(p, plugin, economyService, menu);
        });

        menu.AddItem("Zeus x27", (p, option) =>
        {
            PaintKitMenu.Open(p, plugin, economyService, SkinCatalog.Weapons["Zeus x27"], menu);
        });

        menu.Display(player, 0);
    }
}
