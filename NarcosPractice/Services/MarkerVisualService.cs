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
        if (_markerEntities.TryGetValue(marker.Id, out var existing) && existing.IsValid)
            existing.Remove();

        // No label text at all - just a floating shape, closer to how Yprac marks
        // stand spots with a small gem/diamond prop rather than a readable sign.
        // A real 3D prop or particle effect would need precaching a specific
        // asset path server-side, which is exactly the kind of unverified native
        // resource guess that caused the earlier crash - this reuses the same
        // point_worldtext entity that's already proven safe, just rendering a
        // single glyph instead of a label.
        var pos = new Vector(marker.PosX, marker.PosY, marker.PosZ + 8f);
        var entity = CreateWorldText("◆", pos, Color.FromArgb(255, 90, 170, 255));
        string label = marker.Lineups.Count == 1 ? marker.Lineups[0].Name : $"{marker.Lineups.Count} lineups";
        if (entity != null)
        {
            _markerEntities[marker.Id] = entity;
            Server.PrintToConsole($"[NarcosPractice] Spawned marker '{label}' at {pos.X:F0},{pos.Y:F0},{pos.Z:F0}");
        }
        else
        {
            Server.PrintToConsole($"[NarcosPractice] FAILED to spawn marker '{label}' - CreateEntityByName/property assignment returned null or threw.");
        }
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
            // No background plate and no label anymore - just a large glowing
            // glyph, so size it like a shape (bigger) rather than readable text.
            entity.FontSize = 60;
            entity.Color = color;
            entity.Fullbright = true;
            entity.WorldUnitsPerPx = 0.35f;
            entity.DepthOffset = 0f;
            entity.DrawBackground = false;
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
