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
        RegisterListener<OnMapStart>(mapName =>
        {
            _currentMap = mapName;

            // Give the new level a couple seconds to finish initializing before
            // spawning entities - doing it immediately on OnMapStart is what
            // likely caused the earlier server.dll crash (dedicated servers can
            // fire this for an internal pre-map before the real one is ready).
            AddTimer(2.0f, RefreshMarkerVisuals);
        });
        RegisterListener<OnTick>(OnGameTick);

        // CS2 clears out dynamically-spawned entities during the warmup-to-live
        // round transition - our markers spawn once on map start, well before that
        // transition happens, so they can get wiped before anyone ever sees them.
        // Respawning on every round start is cheap (the queue just drains a few
        // per tick) and guarantees the markers survive past that cleanup.
        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            RefreshMarkerVisuals();
            return HookResult.Continue;
        });

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

    private void RefreshMarkerVisuals()
    {
        if (_markerService == null || _markerVisualService == null)
            return;

        _markerVisualService.RefreshMarkersForMap(_markerService.GetMarkers(_currentMap));
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

        _markerVisualService?.ProcessSpawnQueue();

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
