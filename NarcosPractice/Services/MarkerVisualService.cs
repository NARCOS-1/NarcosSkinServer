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
    // No more per-lineup name labels - back to one icon per marker, no text at
    // all. Multiple lineups sharing a stand spot don't need multiple stacked
    // copies of the same icon, so this is one entity per marker again.
    private readonly Dictionary<string, CPointWorldText> _markerEntities = new();

    // A marker with several lineups shows one aim dot per lineup at once now,
    // not just whichever one happened to match your currently equipped nade.
    private readonly Dictionary<int, List<CPointWorldText>> _aimReferenceEntities = new();
    private readonly Queue<Marker> _pendingSpawns = new();

    // Hollow square for "stand here" (sized to roughly frame a player's own
    // footprint), bullseye ring for "aim here" - plain glyphs rendered through
    // the same point_worldtext entity already proven safe, not a texture/
    // sprite (that would need an unverified native resource reference, same
    // category of guess that caused the earlier crash).
    private const string StandIcon = "□"; // □
    private const string AimIcon = "◎";   // ◎

    // CS2 players are ~32 units wide - FontSize 130 at WorldUnitsPerPx 0.3
    // renders around 39 units, comfortably framing the player's own footprint
    // rather than just barely matching it. The aim dot stays smaller since
    // it's just a point reference, not something to be physically contained by.
    private const int StandIconFontSize = 130;
    private const int DefaultFontSize = 50;

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

        // Sitting close to the actual floor reads as "stand here". REORIENT_NONE
        // keeps whatever angle it's spawned with fixed forever instead of
        // billboarding to face the player (AROUND_UP), and the un-rotated
        // QAngle(0,0,0) plane already lies flat - so this now sits still on the
        // ground as a real footprint decal instead of rotating to face you as
        // you walk around it (which read as "the marker is moving with me").
        var pos = new Vector(marker.PosX, marker.PosY, marker.PosZ + 6f);
        var entity = CreateWorldText(StandIcon, pos, Color.FromArgb(255, 90, 170, 255), background: false,
            StandIconFontSize, PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE);

        if (entity != null)
        {
            _markerEntities[marker.Id] = entity;
            Server.PrintToConsole($"[NarcosPractice] Spawned marker icon at {pos.X:F0},{pos.Y:F0},{pos.Z:F0}");
        }
        else
        {
            Server.PrintToConsole("[NarcosPractice] FAILED to spawn marker icon - CreateEntityByName/property assignment returned null or threw.");
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

    // The "aim here" reference point(s) shown while at/guided to a marker,
    // separate from the persistent stand-here markers above. One set per player -
    // a marker with several lineups gets one dot per lineup, all visible together.
    public void ShowAimReference(int playerSlot, Vector position) =>
        ShowAimReferences(playerSlot, [position]);

    public void ShowAimReferences(int playerSlot, IReadOnlyList<Vector> positions)
    {
        HideAimReference(playerSlot);

        var entities = new List<CPointWorldText>(positions.Count);
        foreach (var position in positions)
        {
            // Reverted the QAngle(90,0,0) base angle that used to be here: it
            // fixed the squashed-circle look, but confirmed via a live position
            // readout that it also shifts the rendered glyph tens of units away
            // from the entity's actual AbsOrigin - the same coordinate the aim
            // hit-test correctly uses. A crooked-looking icon is a far smaller
            // problem than "the visible target isn't where it logically is",
            // so this goes back to the plain, position-accurate orientation.
            var entity = CreateWorldText(AimIcon, position, Color.FromArgb(255, 255, 221, 0), background: false);
            if (entity != null)
                entities.Add(entity);
        }

        if (entities.Count > 0)
            _aimReferenceEntities[playerSlot] = entities;
    }

    public void HideAimReference(int playerSlot)
    {
        if (_aimReferenceEntities.TryGetValue(playerSlot, out var entities))
        {
            foreach (var entity in entities)
            {
                if (entity.IsValid)
                    entity.Remove();
            }

            _aimReferenceEntities.Remove(playerSlot);
        }
    }

    private static CPointWorldText? CreateWorldText(string text, Vector position, Color color, bool background,
        int fontSize = DefaultFontSize,
        PointWorldTextReorientMode_t reorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_AROUND_UP)
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
            // These are icon glyphs now, not readable text labels - sized to
            // read as a shape from a distance rather than as small print.
            entity.FontSize = fontSize;
            entity.Color = color;
            entity.Fullbright = true;
            entity.WorldUnitsPerPx = 0.3f;
            entity.DepthOffset = 0f;
            entity.DrawBackground = background;
            entity.BackgroundBorderWidth = 0.12f;
            entity.BackgroundBorderHeight = 0.2f;
            entity.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
            entity.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
            // NONE pins the text to whatever angle it was spawned with, forever -
            // fine (even wanted) for a flat ground decal that shouldn't rotate as
            // you walk around it. AROUND_UP instead billboards it to continuously
            // face the player (rotating around the vertical axis), which is what
            // a sign-like marker viewed from any angle needs, but reads as "the
            // marker is moving with me" for something meant to sit still on the
            // ground - that's why the stand square uses NONE while the aim dot
            // (which does need to stay legible from any angle at a distance)
            // keeps AROUND_UP.
            entity.ReorientMode = reorientMode;

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
