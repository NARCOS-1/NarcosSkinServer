using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CS2MenuManager.API.Menu;
using NarcosSkinServer.Data;
using NarcosSkinServer.Services;
using System.Linq;

namespace NarcosSkinServer.Menus.Catagories;

public static class AgentMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, EconomyService economyService, WasdMenu? previousMenu = null)
    {
        var menu = new WasdMenu("Agents", plugin)
        {
            PrevMenu = previousMenu
        };

        menu.AddItem("Counter-Terrorist", (p, o) =>
        {
            OpenTeam(p, plugin, economyService, CsTeam.CounterTerrorist, menu);
        });

        menu.AddItem("Terrorist", (p, o) =>
        {
            OpenTeam(p, plugin, economyService, CsTeam.Terrorist, menu);
        });

        menu.Display(player, 0);
    }

    private static void OpenTeam(CCSPlayerController player, BasePlugin plugin, EconomyService economyService, CsTeam team, WasdMenu previousMenu)
    {
        var menu = new WasdMenu(team == CsTeam.CounterTerrorist ? "CT Agents" : "T Agents", plugin)
        {
            PrevMenu = previousMenu
        };

        foreach (var agent in AgentCatalog.Agents.Where(a => a.Team == team))
        {
            menu.AddItem(agent.Name, (p, o) =>
            {
                economyService.ApplyAgent(p, agent);
                p.PrintToChat($"[Narcos] Agent set to {agent.Name}.");
            });
        }

        menu.Display(player, 0);
    }
}
