using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using NarcosPractice.Models;

namespace NarcosPractice.Services;

// Spawns the actual visible markers players walk up to and aim at. Uses CS2's
// native point_worldtext entity (plain 3D text, no model file) rather than a
// prop/model - Agents already proved guessing at .vmdl paths is a great way to
// end up with an invisible or ERROR-placeholder marker.
public class MarkerVisualService
{
    private readonly Dictionary<string, CPointWorldText> _markerEntities = new();
    private readonly Dictionary<int, CPointWorldText> _aimReferenceEntities = new();
    private readonly Queue<Marker> _pendingSpawns = new();

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
        if (_markerEntities.TryGetValue(marker.Id, out var existing) && existing.IsValid)
            existing.Remove();

        string text = marker.Lineups.Count == 1
            ? $"◆ {marker.Lineups[0].Name}"
            : $"◆ {marker.Lineups.Count} lineups";

        var entity = CreateWorldText(text, new Vector(marker.PosX, marker.PosY, marker.PosZ + 8f), Color.FromArgb(255, 143, 211, 255));
        if (entity != null)
            _markerEntities[marker.Id] = entity;
    }

    public void RemoveMarkerText(string markerId)
    {
        if (_markerEntities.TryGetValue(markerId, out var entity))
        {
            if (entity.IsValid)
                entity.Remove();
            _markerEntities.Remove(markerId);
        }
    }

    // The small "aim here" reference point shown while a lineup is actively guided,
    // separate from the persistent stand-here markers above. One per player.
    public void ShowAimReference(int playerSlot, Vector position)
    {
        HideAimReference(playerSlot);

        var entity = CreateWorldText("●", position, Color.FromArgb(255, 120, 255, 120));
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

    private static CPointWorldText? CreateWorldText(string text, Vector position, Color color)
    {
        var entity = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
        if (entity == null)
            return null;

        entity.MessageText = text;
        entity.Enabled = true;
        entity.FontSize = 30;
        entity.Color = color;
        entity.Fullbright = true;
        entity.WorldUnitsPerPx = 0.4f;
        entity.DepthOffset = 0f;
        entity.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
        entity.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
        entity.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

        entity.Teleport(position, new QAngle(0, 0, 0));
        entity.DispatchSpawn();

        return entity;
    }
}
