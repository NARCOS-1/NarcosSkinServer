using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using NarcosPractice.Models;
using static CounterStrikeSharp.API.Core.Listeners;

namespace NarcosPractice;

public partial class Plugin
{
    private string _currentMap = "unknown";

    private void RegisterListeners()
    {
        RegisterListener<OnMapStart>(mapName => _currentMap = mapName);

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
}
