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

    public void RefreshMarkersForMap(List<Marker> markers)
    {
        foreach (var entity in _markerEntities.Values)
        {
            if (entity.IsValid)
                entity.Remove();
        }
        _markerEntities.Clear();

        foreach (var marker in markers)
            SpawnMarkerText(marker);
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
