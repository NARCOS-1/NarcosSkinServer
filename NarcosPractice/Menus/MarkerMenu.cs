using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosPractice.Models;
using NarcosPractice.Services;

namespace NarcosPractice.Menus;

public static class MarkerMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, PracticeService practiceService, Marker marker)
    {
        var menu = new WasdMenu("Lineups Here", plugin);

        foreach (var lineup in marker.Lineups)
        {
            menu.AddItem($"{lineup.Name} [{lineup.Type}, {lineup.Technique}, {lineup.Strength}]", (p, o) =>
            {
                practiceService.GuideTo(p, lineup);
            });
        }

        menu.Display(player, 0);
    }
}
