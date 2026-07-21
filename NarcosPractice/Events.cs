using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using NarcosPractice.Menus;
using NarcosPractice.Models;
using static CounterStrikeSharp.API.Core.Listeners;

namespace NarcosPractice;

public partial class Plugin
{
    private string _currentMap = "unknown";

    private void RegisterListeners()
    {
        RegisterListener<OnMapStart>(mapName => _currentMap = mapName);
        RegisterListener<OnTick>(OnGameTick);

        RegisterEventHandler<EventSmokegrenadeDetonate>((@event, info) =>
        {
            HandleDetonate(@event.Userid, NadeType.Smoke, @event.X, @event.Y, @event.Z);
            return HookResult.Continue;
        });

        RegisterEventHandler<EventHegrenadeDetonate>((@event, info) =>
        {
            HandleDetonate(@event.Userid, NadeType.HE, @event.X, @event.Y, @event.Z);
            return HookResult.Continue;
        });

        RegisterEventHandler<EventFlashbangDetonate>((@event, info) =>
        {
            HandleDetonate(@event.Userid, NadeType.Flash, @event.X, @event.Y, @event.Z);
            return HookResult.Continue;
        });

        RegisterEventHandler<EventMolotovDetonate>((@event, info) =>
        {
            HandleDetonate(@event.Userid, NadeType.Molotov, @event.X, @event.Y, @event.Z);
            return HookResult.Continue;
        });
    }

    private void HandleDetonate(CCSPlayerController? player, NadeType type, float x, float y, float z)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        _practiceService?.CompletePendingSave(player, _currentMap, type, new Vector(x, y, z));
    }

    // Drives the "walk up to a marker, press E" interaction for every connected player.
    private void OnGameTick()
    {
        if (_practiceService == null)
            return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot || !player.PawnIsAlive)
                continue;

            _practiceService.Tick(player, _currentMap, marker =>
            {
                MarkerMenu.Open(player, this, _practiceService, marker);
            });
        }
    }
}
