using System.Collections.Concurrent;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using NarcosPractice.Models;

namespace NarcosPractice.Services;

public class PracticeService
{
    private const float PendingSaveTimeoutSeconds = 10f;

    // How far along the saved aim direction the "look here" reference dot sits.
    // Not the real detonation point (could be far off or behind a wall) - just a
    // visible anchor close enough to always render.
    private const float AimReferenceDistance = 400f;

    // How far away a marker can be and still count as "aimed at" for the
    // shoot/use-to-teleport fast-travel convenience, and how close the aim ray
    // itself has to pass to the marker's actual position (roughly the icon's
    // own world-space footprint, not a generous fixed-degree cone).
    private const float AimMaxDistance = 1200f;
    private const float AimHitRadius = 18f;

    // Close enough to a marker to count as "standing at it" and see every one of
    // its lineups' aim dots at once - this is the primary way lineups are meant
    // to be practiced; shoot/use-to-teleport above is just a fast-travel shortcut.
    private const float StandingAtMarkerRadius = 100f;

    private record PendingSave(string Name, NadeType Type, ThrowTechnique Technique, ThrowStrength Strength, Vector ThrowPos, QAngle ThrowAngles, DateTime ArmedAt);

    private readonly ConcurrentDictionary<int, PendingSave> _pending = new();

    // Rising-edge tracking for the interact (Use/Attack) input, per player slot.
    private readonly ConcurrentDictionary<int, bool> _interactedLastTick = new();

    // Last lineup a player was guided to, so !nadereset can put them right back
    // without walking back or re-opening the menu.
    private readonly ConcurrentDictionary<int, Lineup> _lastGuided = new();

    private readonly ConcurrentDictionary<int, bool> _noclip = new();

    // Last hint text actually sent to each player, so we only call
    // PrintToCenterHtml when it changes instead of every single tick - sending
    // it repeatedly (even as "") is what kept the hint box frame stuck on
    // screen permanently instead of clearing when there was nothing to show.
    private readonly ConcurrentDictionary<int, string> _lastCenterText = new();

    // Which marker's aim dots are currently shown to each player, so they're
    // only respawned when the player actually walks up to a different marker,
    // not every single tick.
    private readonly ConcurrentDictionary<int, string> _lastShownMarkerId = new();

    // Which of a marker's several lineups is currently considered "aimed at"
    // per player - used purely for hysteresis in FindAimedAtLineup, so normal
    // crosshair wobble right at the edge of the angle threshold doesn't flicker
    // the technique bar on and off every other tick.
    private readonly ConcurrentDictionary<int, Lineup> _lastAimedLineup = new();

    private readonly MarkerService _markerService;
    private readonly MarkerVisualService _markerVisualService;

    public PracticeService(MarkerService markerService, MarkerVisualService markerVisualService)
    {
        _markerService = markerService;
        _markerVisualService = markerVisualService;
    }

    public void ArmSave(CCSPlayerController player, string name, NadeType type, ThrowTechnique technique, ThrowStrength strength)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.EyeAngles == null)
            return;

        _pending[player.Slot] = new PendingSave(
            name,
            type,
            technique,
            strength,
            new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
            new QAngle(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z),
            DateTime.UtcNow);

        player.PrintToChat($"[Practice] Armed lineup save '{name}' ({type}, {technique}, {strength}) - throw the nade for real now.");
    }

    // Called from the relevant *_detonate game event handler once it's confirmed to
    // belong to this player and matches the pending save's nade type. This captures
    // where your own real throw actually landed - nothing is faked or computed.
    public void CompletePendingSave(CCSPlayerController player, string map, NadeType type, Vector detonatePos)
    {
        if (!_pending.TryGetValue(player.Slot, out var pending) || pending.Type != type)
            return;

        _pending.TryRemove(player.Slot, out _);

        if ((DateTime.UtcNow - pending.ArmedAt).TotalSeconds > PendingSaveTimeoutSeconds)
        {
            player.PrintToChat("[Practice] Lineup save expired - run !nadesave again right before throwing.");
            return;
        }

        var lineup = new Lineup
        {
            Name = pending.Name,
            Type = pending.Type,
            Technique = pending.Technique,
            Strength = pending.Strength,
            ThrowPosX = pending.ThrowPos.X,
            ThrowPosY = pending.ThrowPos.Y,
            ThrowPosZ = pending.ThrowPos.Z,
            ThrowAngPitch = pending.ThrowAngles.X,
            ThrowAngYaw = pending.ThrowAngles.Y,
            DetonatePosX = detonatePos.X,
            DetonatePosY = detonatePos.Y,
            DetonatePosZ = detonatePos.Z
        };

        var marker = _markerService.GetOrCreateMarker(map, pending.ThrowPos.X, pending.ThrowPos.Y, pending.ThrowPos.Z);
        _markerService.AddLineup(map, marker, lineup);
        _markerVisualService.SpawnMarkerText(marker);

        player.PrintToChat($"[Practice] Saved {type} lineup '{pending.Name}' to a marker here.");
    }

    // The core practice loop: teleport to the stand spot, face the saved aim angle,
    // give the correct (empty) nade so it's in hand, show a "look here" reference
    // dot, and put up the exact technique/strength on screen. You throw it yourself
    // - this only sets up the attempt.
    public void GuideTo(CCSPlayerController player, Lineup lineup)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var throwPos = new Vector(lineup.ThrowPosX, lineup.ThrowPosY, lineup.ThrowPosZ);
        var throwAngles = new QAngle(lineup.ThrowAngPitch, lineup.ThrowAngYaw, 0);

        // Confirmed cause of the "weird standing" bug: Teleport's angle argument
        // sets the pawn's own body orientation, and passing the saved pitch
        // (often steep on jumpthrows) tipped the whole body model over instead
        // of just aiming the camera. Only yaw goes here now - the aim reference
        // marker below already shows the exact saved pitch+yaw to look at, so
        // the player still gets the full 3D aim direction without the server
        // forcing their view (which CS2Sharp doesn't cleanly support anyway -
        // EyeAngles has no public setter, and writing m_angEyeAngles directly
        // via schema crashed the server since it isn't a networked field).
        pawn.Teleport(throwPos, new QAngle(0, throwAngles.Y, 0), new Vector(0, 0, 0));

        string weaponClass = lineup.Type switch
        {
            NadeType.Smoke => "weapon_smokegrenade",
            NadeType.Flash => "weapon_flashbang",
            NadeType.HE => "weapon_hegrenade",
            NadeType.Molotov => "weapon_molotov",
            _ => "weapon_smokegrenade"
        };

        player.GiveNamedItem(weaponClass);

        _lastGuided[player.Slot] = lineup;
        _markerVisualService.ShowAimReference(player.Slot, ResolveAimReferencePoint(lineup, throwPos, throwAngles));

        string notes = CleanNotes(lineup.Notes, " - ");
        string notesSuffix = string.IsNullOrWhiteSpace(notes) ? "" : $" ({notes})";
        player.PrintToChat($"[Practice] '{lineup.Name}' - {lineup.Technique}, {lineup.Strength} throw.{notesSuffix} Line up and throw it for real.");
    }

    // Instantly puts you back at the same stand spot/angle/nade without walking back
    // or re-opening the menu - matches Yprac's fast reset-and-retry loop.
    public void Reset(CCSPlayerController player)
    {
        if (_lastGuided.TryGetValue(player.Slot, out var lineup))
            GuideTo(player, lineup);
        else
            player.PrintToChat("[Practice] Nothing to reset to yet - pick a lineup from a marker or !nades first.");
    }

    // Toggles noclip so you can fly up and check where your throw actually landed,
    // then drop back down - done via MoveType directly rather than the "noclip"
    // console command, which is a plain toggle and would fight with our own state.
    public void ToggleVerify(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        bool goingNoclip = !_noclip.GetOrAdd(player.Slot, false);
        _noclip[player.Slot] = goingNoclip;

        Schema.GetRef<MoveType_t>(pawn.Handle, "CBaseEntity", "m_MoveType") =
            goingNoclip ? MoveType_t.MOVETYPE_NOCLIP : MoveType_t.MOVETYPE_WALK;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");

        // Only take mouse-wheel-down over while actually flying, so a personal
        // mwheeldown bind (e.g. scroll-to-jump) is untouched the rest of the time.
        player.ExecuteClientCommand(goingNoclip
            ? "bind \"MWHEELDOWN\" \"css_verify\""
            : "bind \"MWHEELDOWN\" \"+jump\"");

        player.PrintToChat(goingNoclip
            ? "[Practice] Noclip on - fly up to check your throw, scroll wheel down to drop back down."
            : "[Practice] Noclip off.");
    }

    // Primary flow: standing at a marker shows an aim dot for every lineup there
    // at once (not just whichever matches the currently equipped nade) - looking
    // at a specific dot shows that lineup's technique bar. Secondary: aiming at a
    // marker you're not standing at still offers shoot/use as a fast-travel
    // shortcut, independent of whether you happen to be near a *different*
    // marker right now. Called once per tick from Events.cs.
    public void Tick(CCSPlayerController player, string map, Action<Marker> onInteract)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.EyeAngles == null)
            return;

        bool interactingNow = (player.Buttons & (PlayerButtons.Use | PlayerButtons.Attack)) != 0;
        bool interactedLastTick = _interactedLastTick.GetOrAdd(player.Slot, false);
        _interactedLastTick[player.Slot] = interactingNow;

        var eyeOrigin = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + 64f);
        var forward = DirectionFromAngles(pawn.EyeAngles.X, pawn.EyeAngles.Y);

        var nearbyMarker = _markerService.FindNearest(map, pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z, StandingAtMarkerRadius);
        var aimedMarker = FindClosestAlongRay(eyeOrigin, forward, _markerService.GetMarkers(map),
            m => new Vector(m.PosX, m.PosY, m.PosZ), AimMaxDistance, AimHitRadius);

        // A marker you're actually pointing at offers the shoot/use shortcut
        // regardless of being close to some *other* marker right now - that
        // proximity used to swallow the interaction outright, which is what
        // made shoot-to-teleport feel broken at close range.
        if (aimedMarker != null && aimedMarker != nearbyMarker)
        {
            // Don't leave a stale aim dot / marker-id on screen from whatever
            // was shown a moment ago while switching into this branch.
            _markerVisualService.HideAimReference(player.Slot);
            _lastShownMarkerId.TryRemove(player.Slot, out _);

            int count = aimedMarker.Lineups.Count;
            SetCenterText(player, $"<font color='#8fd3ff'>SHOOT or USE</font> to teleport - {count} lineup{(count == 1 ? "" : "s")} here");

            if (interactingNow && !interactedLastTick)
                onInteract(aimedMarker);
            return;
        }

        if (nearbyMarker != null)
        {
            ShowStandingGuide(player, nearbyMarker, eyeOrigin, forward);
            return;
        }

        _markerVisualService.HideAimReference(player.Slot);
        _lastShownMarkerId.TryRemove(player.Slot, out _);
        SetCenterText(player, "");
    }

    // Shows one aim dot per lineup at this marker, all at once - only respawns
    // them when the player's walked up to a different marker, not every tick.
    // Whichever dot the player is currently looking at gets its technique bar
    // shown; otherwise just a headcount of what's available here.
    private void ShowStandingGuide(CCSPlayerController player, Marker marker, Vector eyeOrigin, Vector forward)
    {
        if (!_lastShownMarkerId.TryGetValue(player.Slot, out var previousId) || previousId != marker.Id)
        {
            var positions = marker.Lineups
                .Select(l => ResolveAimReferencePoint(l, new Vector(l.ThrowPosX, l.ThrowPosY, l.ThrowPosZ), new QAngle(l.ThrowAngPitch, l.ThrowAngYaw, 0)))
                .ToList();

            _markerVisualService.ShowAimReferences(player.Slot, positions);
            _lastShownMarkerId[player.Slot] = marker.Id;
        }

        var aimedLineup = FindAimedAtLineup(player.Slot, eyeOrigin, forward, marker.Lineups);

        if (aimedLineup != null)
        {
            _lastGuided[player.Slot] = aimedLineup;
            SetCenterText(player, BuildTechniqueBarText(aimedLineup));
            return;
        }

        int count = marker.Lineups.Count;
        string types = string.Join(", ", marker.Lineups.Select(l => l.Type).Distinct());
        SetCenterText(player, $"<font color='#8fd3ff'>{count} lineup{(count == 1 ? "" : "s")} here</font> ({types}) - look at a marker for details");
    }

    // Aim-reference dots are often far away (a window across a rooftop, the far
    // side of a sightline) - the tight, fixed-radius hitscan used for shooting
    // a nearby diamond marker doesn't work here, since at long range even
    // dead-center aim has enough natural eye-height/angle slop to miss an
    // 18-unit-wide target entirely. This uses an angular tolerance instead
    // (unaffected by distance), which is also the right tool for "which of
    // several aim dots is roughly under my crosshair" rather than requiring
    // real hitscan precision.
    //
    // Two thresholds instead of one: ordinary crosshair wobble bounces right
    // at the edge of a single fixed angle every tick, so a lineup would pass
    // and fail the check from one tick to the next - flickering the technique
    // bar on and off ("split second"). Once a lineup is locked on, it keeps
    // being reported as long as it's within the wider Exit angle, and only
    // loses that lock to another lineup that's inside the tighter Enter angle.
    private const float LineupAimEnterAngleDegrees = 8f;
    private const float LineupAimExitAngleDegrees = 14f;

    private Lineup? FindAimedAtLineup(int playerSlot, Vector eyeOrigin, Vector forward, IEnumerable<Lineup> lineups)
    {
        _lastAimedLineup.TryGetValue(playerSlot, out var current);

        Lineup? best = null;
        float bestAngle = float.MaxValue;

        foreach (var lineup in lineups)
        {
            var aimPoint = ResolveAimReferencePoint(lineup,
                new Vector(lineup.ThrowPosX, lineup.ThrowPosY, lineup.ThrowPosZ),
                new QAngle(lineup.ThrowAngPitch, lineup.ThrowAngYaw, 0));

            float dx = aimPoint.X - eyeOrigin.X;
            float dy = aimPoint.Y - eyeOrigin.Y;
            float dz = aimPoint.Z - eyeOrigin.Z;
            float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            if (dist < 1f)
                continue;

            float dot = (forward.X * dx + forward.Y * dy + forward.Z * dz) / dist;
            dot = Math.Clamp(dot, -1f, 1f);
            float angleDeg = MathF.Acos(dot) * (180f / MathF.PI);

            float threshold = ReferenceEquals(lineup, current) ? LineupAimExitAngleDegrees : LineupAimEnterAngleDegrees;
            if (angleDeg < threshold && angleDeg < bestAngle)
            {
                bestAngle = angleDeg;
                best = lineup;
            }
        }

        if (best != null)
            _lastAimedLineup[playerSlot] = best;
        else
            _lastAimedLineup.TryRemove(playerSlot, out _);

        return best;
    }

    // Only actually calls PrintToCenterHtml when the text differs from what
    // this player was last shown - repeatedly re-sending identical text (even
    // "") every tick is what kept the hint box permanently on screen.
    private void SetCenterText(CCSPlayerController player, string text)
    {
        if (_lastCenterText.TryGetValue(player.Slot, out var last) && last == text)
            return;

        _lastCenterText[player.Slot] = text;
        player.PrintToCenterHtml(text);
    }

    // Simulated hitscan rather than a real engine trace: projects each candidate
    // onto the aim ray and only counts it as "aimed at" if the ray actually
    // passes within its footprint (hitRadius), picking whichever qualifying
    // candidate is nearest along the ray - same behavior as a real trace
    // hitting the closest thing first. Shared by the marker check (used for
    // the shoot/use-to-teleport shortcut) and the per-lineup check (used to
    // pick which of a marker's several aim dots the player is looking at).
    // The previous marker version picked whichever had the smallest angle to
    // it, which is why it felt so imprecise: a fixed-degree cone covers a
    // radius that grows with distance, so something 1200 units away could
    // register from being tens of units off to the side while something close
    // needed near-pixel accuracy.
    private static T? FindClosestAlongRay<T>(Vector eyeOrigin, Vector forward, IEnumerable<T> candidates,
        Func<T, Vector> position, float maxDistance, float hitRadius) where T : class
    {
        T? best = null;
        float bestDistanceAlongRay = maxDistance;

        foreach (var candidate in candidates)
        {
            var pos = position(candidate);
            float dx = pos.X - eyeOrigin.X;
            float dy = pos.Y - eyeOrigin.Y;
            float dz = pos.Z - eyeOrigin.Z;

            float distanceAlongRay = dx * forward.X + dy * forward.Y + dz * forward.Z;
            if (distanceAlongRay <= 0f || distanceAlongRay >= bestDistanceAlongRay)
                continue;

            float closestX = eyeOrigin.X + forward.X * distanceAlongRay;
            float closestY = eyeOrigin.Y + forward.Y * distanceAlongRay;
            float closestZ = eyeOrigin.Z + forward.Z * distanceAlongRay;

            float perpX = pos.X - closestX;
            float perpY = pos.Y - closestY;
            float perpZ = pos.Z - closestZ;
            float perpDist = MathF.Sqrt(perpX * perpX + perpY * perpY + perpZ * perpZ);

            if (perpDist > hitRadius)
                continue;

            bestDistanceAlongRay = distanceAlongRay;
            best = candidate;
        }

        return best;
    }

    // Prefers the real authored aim point from the source data over the
    // generic projected fallback - see Lineup.AimPosX/Y/Z for why this exists.
    private static Vector ResolveAimReferencePoint(Lineup lineup, Vector throwPos, QAngle throwAngles)
    {
        if (lineup.AimPosX.HasValue && lineup.AimPosY.HasValue && lineup.AimPosZ.HasValue)
            return new Vector(lineup.AimPosX.Value, lineup.AimPosY.Value, lineup.AimPosZ.Value);

        return ComputeAimReferencePoint(throwPos, throwAngles);
    }

    private static Vector ComputeAimReferencePoint(Vector throwPos, QAngle throwAngles)
    {
        var direction = DirectionFromAngles(throwAngles.X, throwAngles.Y);
        return new Vector(
            throwPos.X + direction.X * AimReferenceDistance,
            throwPos.Y + direction.Y * AimReferenceDistance,
            throwPos.Z + direction.Z * AimReferenceDistance);
    }

    private static Vector DirectionFromAngles(float pitchDegrees, float yawDegrees)
    {
        float pitchRad = pitchDegrees * (MathF.PI / 180f);
        float yawRad = yawDegrees * (MathF.PI / 180f);

        return new Vector(
            MathF.Cos(yawRad) * MathF.Cos(pitchRad),
            MathF.Sin(yawRad) * MathF.Cos(pitchRad),
            -MathF.Sin(pitchRad));
    }

    private static string BuildTechniqueBarText(Lineup lineup)
    {
        string techniquePart = lineup.Technique switch
        {
            ThrowTechnique.Jumpthrow => "JUMP | THROW",
            ThrowTechnique.Walkthrow => "W | THROW",
            ThrowTechnique.Runjumpthrow => "SHIFT+W | JUMP | THROW",
            ThrowTechnique.Duckthrow => "CROUCH | THROW",
            _ => "THROW"
        };

        string strengthPart = lineup.Strength switch
        {
            ThrowStrength.Medium => " (medium click)",
            ThrowStrength.Short => " (short click)",
            _ => ""
        };

        string notes = CleanNotes(lineup.Notes, "<br>");
        string notesLine = string.IsNullOrWhiteSpace(notes) ? "" : $"<br><font color='#cccccc'>{notes}</font>";

        return $"<font color='#ffcc66'>&lt; {techniquePart} &gt;</font>{strengthPart}{notesLine}";
    }

    // Source annotation data has literal "\n" (backslash-n) two-character
    // sequences the original author typed as their own formatting, not real
    // newline characters - shown raw, they look exactly like that: a visible
    // backslash and n in the middle of the text. Replace with something
    // sensible for wherever it's being displayed (plain chat vs HTML center text).
    private static string CleanNotes(string? notes, string lineBreakReplacement)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return "";

        return notes.Replace("\\n", lineBreakReplacement).Trim();
    }
}
