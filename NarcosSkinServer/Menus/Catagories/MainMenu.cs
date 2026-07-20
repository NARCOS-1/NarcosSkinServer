using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
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
            p.PrintToChat("[Narcos] Pistols selected.");
        });

        menu.AddItem("SMGs", (p, option) =>
        {
            p.PrintToChat("[Narcos] SMGs selected.");
        });

        menu.AddItem("Rifles", (p, option) =>
        {
            RifleMenu.Open(p, plugin, economyService, menu);
        });

        menu.AddItem("Snipers", (p, option) =>
        {
            p.PrintToChat("[Narcos] Snipers selected.");
        });

        menu.AddItem("Heavy", (p, option) =>
        {
            p.PrintToChat("[Narcos] Heavy selected.");
        });

        menu.Display(player, 0);
    }
}
