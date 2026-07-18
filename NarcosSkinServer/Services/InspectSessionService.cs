using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using NarcosSkinServer.Models;

namespace NarcosSkinServer.Services;

public class InspectSessionService
{
    private readonly ConcurrentDictionary<ulong, InspectSession> _sessions = new();

    public InspectSession Get(CCSPlayerController player)
    {
        return _sessions.GetOrAdd(player.SteamID, _ => new InspectSession());
    }

    public void Remove(CCSPlayerController player)
    {
        _sessions.TryRemove(player.SteamID, out _);
    }
}