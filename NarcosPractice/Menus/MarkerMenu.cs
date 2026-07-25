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
                // Teleporting in the same tick the world-space menu closes seems
                // to leave the client's camera stuck looking from outside the
                // player (third-person, spectator-style nameplate) even though
                // movement/shooting still work fine - a short delay lets the
                // menu's own close/camera-reattach finish first.
                plugin.AddTimer(0.1f, () => practiceService.GuideTo(p, lineup));
            });
        }

        menu.Display(player, 0);
    }
}
