using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosPractice.Models;
using NarcosPractice.Services;

namespace NarcosPractice.Menus;

public static class MarkerMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, PracticeService practiceService, Marker marker)
    {
        var menu = new WasdMenu("Lineups Here", plugin)
        {
            // Selecting an item here immediately teleports via GuideTo. Leaving
            // FreezePlayer on means the library freezes via MoveType every open
            // tick and only unfreezes in Close() right after our own callback -
            // reconnect-only-fix symptoms pointed at that MoveType dance itself
            // being the problem, not its timing, so skip it entirely instead of
            // trying to race it.
            WasdMenu_FreezePlayer = false
        };

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
