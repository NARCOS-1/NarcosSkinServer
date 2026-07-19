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
            KnifeMenu.Open(player, plugin, economyService);
        });

        menu.AddItem("Gloves", (p, option) =>
        {
            p.PrintToChat("[Narcos] Gloves callback");
            GloveMenu.Open(p, plugin, economyService);
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
            p.PrintToChat("[Narcos] Rifles selected.");
        });

        menu.AddItem("Snipers", (p, option) =>
        {
            p.PrintToChat("[Narcos] Snipers selected.");
        });

        menu.AddItem("Heavy", (p, option) =>
        {
            p.PrintToChat("[Narcos] Heavy selected.");
        });
        player.PrintToChat("[Narcos] Display");
        player.PrintToChat("[Narcos] About to call Display()");
        menu.Display(player, 0);
        player.PrintToChat("[Narcos] Display() returned");
    }
}