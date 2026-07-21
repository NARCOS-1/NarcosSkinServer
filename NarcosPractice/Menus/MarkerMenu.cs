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

        if (marker.Lineups.Count > 1)
        {
            menu.AddItem("Run All (one after another)", (p, o) =>
            {
                practiceService.ThrowAll(p, marker.Lineups);
            });
        }

        foreach (var lineup in marker.Lineups)
        {
            menu.AddItem($"{lineup.Name} [{lineup.Type}, {lineup.Technique}, {lineup.Strength}]", (p, o) =>
            {
                practiceService.Throw(p, lineup);
            });
        }

        menu.Display(player, 0);
    }
}
