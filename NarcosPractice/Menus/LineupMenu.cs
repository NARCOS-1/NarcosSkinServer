using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosPractice.Models;
using NarcosPractice.Services;
using System.Linq;

namespace NarcosPractice.Menus;

public static class LineupMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, LineupService lineupService, PracticeService practiceService, string map)
    {
        var menu = new WasdMenu("Nade Practice", plugin);

        foreach (NadeType type in Enum.GetValues<NadeType>())
        {
            menu.AddItem(type.ToString(), (p, o) =>
            {
                OpenType(p, plugin, lineupService, practiceService, map, type, menu);
            });
        }

        menu.Display(player, 0);
    }

    private static void OpenType(CCSPlayerController player, BasePlugin plugin, LineupService lineupService, PracticeService practiceService, string map, NadeType type, WasdMenu previousMenu)
    {
        var lineups = lineupService.GetLineups(map).Where(l => l.Type == type).OrderBy(l => l.Name).ToList();

        var menu = new WasdMenu($"{type} Lineups", plugin)
        {
            PrevMenu = previousMenu
        };

        if (lineups.Count == 0)
        {
            player.PrintToChat($"[Practice] No saved {type} lineups on {map} yet - use !nadesave <name> to add one.");
            return;
        }

        foreach (var lineup in lineups)
        {
            menu.AddItem(lineup.Name, (p, o) =>
            {
                practiceService.Throw(p, lineup);
            });
        }

        menu.Display(player, 0);
    }
}
