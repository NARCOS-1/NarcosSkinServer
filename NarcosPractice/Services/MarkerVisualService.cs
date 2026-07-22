using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using NarcosPractice.Models;

namespace NarcosPractice.Services;

// Spawns the actual visible markers players walk up to and aim at. Uses CS2's
// native point_worldtext entity (plain 3D text with an optional background
// plate, no model file) rather than a prop/particle - guessing at an asset
// path for either of those is exactly the kind of unverified native resource
// reference that caused a real crash earlier in this plugin's history.
public class MarkerVisualService
{
    // One entity per lineup now, not per marker - Yprac shows a separate
    // labeled panel for each lineup ("Smoke wall 3", "Smoke mouz wall 1", ...)
    // even when several share roughly the same stand spot, rather than
    // collapsing them into a single "3 lineups" summary.
    private readonly Dictionary<string, List<CPointWorldText>> _markerEntities = new();
    private readonly Dictionary<int, CPointWorldText> _aimReferenceEntities = new();
    private readonly Queue<Marker> _pendingSpawns = new();

    // Vertical gap between stacked lineup labels at the same marker, so they
    // read as a list rather than overlapping into unreadable mush.
    private const float LabelStackSpacing = 26f;

    // How many point_worldtext entities to create per tick while draining the
    // spawn queue - some maps have 80+ lineups, and creating dozens of entities
    // synchronously in one frame right after a map transition is what actually
    // caused the server.dll access violation, not the entity properties/enums
    // themselves (those match a verified working reference implementation).
    private const int MaxSpawnsPerTick = 4;

    public void RefreshMarkersForMap(List<Marker> markers)
    {
        // Entities from whatever map was previously loaded are already destroyed
        // by the engine during the level transition - calling .Remove() on those
        // stale handles ourselves is a use-after-free, not a safe no-op. Just drop
        // our references and let new ones get created fresh for the new map.
        _markerEntities.Clear();
        _aimReferenceEntities.Clear();

        _pendingSpawns.Clear();
        foreach (var marker in markers)
            _pendingSpawns.Enqueue(marker);

        Server.PrintToConsole($"[NarcosPractice] Queued {markers.Count} markers to spawn.");
    }

    // Drains a few pending marker spawns per tick instead of creating every
    // entity for a map in one synchronous burst. Call this from OnTick.
    public void ProcessSpawnQueue()
    {
        for (int i = 0; i < MaxSpawnsPerTick && _pendingSpawns.Count > 0; i++)
            SpawnMarkerText(_pendingSpawns.Dequeue());
    }

    public void SpawnMarkerText(Marker marker)
    {
        RemoveMarkerText(marker.Id);

        var entities = new List<CPointWorldText>();
        for (int i = 0; i < marker.Lineups.Count; i++)
        {
            var lineup = marker.Lineups[i];
            var pos = new Vector(marker.PosX, marker.PosY, marker.PosZ + 32f + i * LabelStackSpacing);
            var entity = CreateWorldText(lineup.Name, pos, Color.FromArgb(255, 255, 193, 61), background: true);

            if (entity != null)
            {
                entities.Add(entity);
                Server.PrintToConsole($"[NarcosPractice] Spawned '{lineup.Name}' at {pos.X:F0},{pos.Y:F0},{pos.Z:F0}");
            }
            else
            {
                Server.PrintToConsole($"[NarcosPractice] FAILED to spawn '{lineup.Name}' - CreateEntityByName/property assignment returned null or threw.");
            }
        }

        if (entities.Count > 0)
            _markerEntities[marker.Id] = entities;
    }

    public void RemoveMarkerText(string markerId)
    {
        if (_markerEntities.TryGetValue(markerId, out var entities))
        {
            foreach (var entity in entities)
            {
                if (entity.IsValid)
                    entity.Remove();
            }
            _markerEntities.Remove(markerId);
        }
    }

    // The small "aim here" reference point shown while a lineup is actively guided,
    // separate from the persistent stand-here markers above. One per player.
    public void ShowAimReference(int playerSlot, Vector position)
    {
        HideAimReference(playerSlot);

        var entity = CreateWorldText("O", position, Color.FromArgb(255, 255, 221, 0), background: false);
        if (entity != null)
            _aimReferenceEntities[playerSlot] = entity;
    }

    public void HideAimReference(int playerSlot)
    {
        if (_aimReferenceEntities.TryGetValue(playerSlot, out var entity))
        {
            if (entity.IsValid)
                entity.Remove();
            _aimReferenceEntities.Remove(playerSlot);
        }
    }

    private static CPointWorldText? CreateWorldText(string text, Vector position, Color color, bool background)
    {
        try
        {
            var entity = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
            if (entity == null)
            {
                Server.PrintToConsole("[NarcosPractice] CreateEntityByName(point_worldtext) returned null.");
                return null;
            }

            entity.MessageText = text;
            entity.Enabled = true;
            entity.FontName = "Arial";
            entity.FontSize = 24;
            entity.Color = color;
            entity.Fullbright = true;
            entity.WorldUnitsPerPx = 0.3f;
            entity.DepthOffset = 0f;
            entity.DrawBackground = background;
            entity.BackgroundBorderWidth = 0.12f;
            entity.BackgroundBorderHeight = 0.2f;
            entity.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
            entity.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
            // NONE pins the text to a fixed facing forever - from most approach
            // angles it's edge-on and invisible. AROUND_UP billboards it to always
            // face the player (rotating only around the vertical axis), which is
            // the only other option this enum has and what a floor marker needs.
            entity.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_AROUND_UP;

            entity.Teleport(position, new QAngle(0, 0, 0));
            entity.DispatchSpawn();

            return entity;
        }
        catch (Exception ex)
        {
            Server.PrintToConsole($"[NarcosPractice] EXCEPTION creating world text: {ex}");
            return null;
        }
    }
}
