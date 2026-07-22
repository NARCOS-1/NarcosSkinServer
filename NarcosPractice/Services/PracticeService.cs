using System.Collections.Concurrent;
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

    // How far away / how far off-crosshair a marker can be and still count as
    // "aimed at" for the shoot/use-to-teleport interaction.
    private const float AimMaxDistance = 1200f;
    private const float AimMaxAngleDegrees = 4f;

    // Once guided to a lineup, the technique bar keeps showing as long as you're
    // still roughly at the stand spot - past this distance you've clearly moved on.
    private const float ActiveGuideRadius = 300f;

    private record PendingSave(string Name, NadeType Type, ThrowTechnique Technique, ThrowStrength Strength, Vector ThrowPos, QAngle ThrowAngles, DateTime ArmedAt);

    private readonly ConcurrentDictionary<int, PendingSave> _pending = new();

    // Rising-edge tracking for the interact (Use/Attack) input, per player slot.
    private readonly ConcurrentDictionary<int, bool> _interactedLastTick = new();

    // Last lineup a player was guided to, so !nadereset can put them right back
    // without walking back or re-opening the menu.
    private readonly ConcurrentDictionary<int, Lineup> _lastGuided = new();

    private readonly ConcurrentDictionary<int, bool> _noclip = new();

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

        pawn.Teleport(throwPos, throwAngles, new Vector(0, 0, 0));

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
        _markerVisualService.ShowAimReference(player.Slot, ComputeAimReferencePoint(throwPos, throwAngles));

        player.PrintToChat($"[Practice] '{lineup.Name}' - {lineup.Technique}, {lineup.Strength} throw. Line up and throw it for real.");
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
    // then drop back down - done via MoveType directly (not the "noclip" console
    // command) so it doesn't get caught by NarcosSkinServer's cheat-command blocker.
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

    // Drives the aim-at-a-marker / technique-bar HUD and the shoot-or-use-to-teleport
    // interaction. Called once per tick from Events.cs.
    public void Tick(CCSPlayerController player, string map, Action<Marker> onInteract)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.EyeAngles == null)
            return;

        bool interactingNow = (player.Buttons & (PlayerButtons.Use | PlayerButtons.Attack)) != 0;
        bool interactedLastTick = _interactedLastTick.GetOrAdd(player.Slot, false);
        _interactedLastTick[player.Slot] = interactingNow;

        // Actively practicing a guided lineup and still near its stand spot - show
        // the technique bar instead of the marker-approach hint.
        if (_lastGuided.TryGetValue(player.Slot, out var activeLineup) &&
            Distance(pawn.AbsOrigin, activeLineup.ThrowPosX, activeLineup.ThrowPosY, activeLineup.ThrowPosZ) <= ActiveGuideRadius)
        {
            player.PrintToCenterHtml(BuildTechniqueBarText(activeLineup));
            return;
        }

        var marker = FindAimedAtMarker(player, map);

        if (marker == null)
        {
            player.PrintToCenterHtml("");
            return;
        }

        int count = marker.Lineups.Count;
        player.PrintToCenterHtml($"<font color='#8fd3ff'>SHOOT or USE</font> to teleport - {count} lineup{(count == 1 ? "" : "s")} here");

        if (interactingNow && !interactedLastTick)
            onInteract(marker);
    }

    // Simple angle-to-nearest-marker check rather than a real engine trace - good
    // enough for "which marker is roughly under your crosshair," and avoids taking
    // on a whole new trace/collision API for this.
    private Marker? FindAimedAtMarker(CCSPlayerController player, string map)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.EyeAngles == null)
            return null;

        var eyeOrigin = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z + 64f);
        var forward = DirectionFromAngles(pawn.EyeAngles.X, pawn.EyeAngles.Y);

        Marker? best = null;
        float bestAngle = AimMaxAngleDegrees;

        foreach (var marker in _markerService.GetMarkers(map))
        {
            float dx = marker.PosX - eyeOrigin.X;
            float dy = marker.PosY - eyeOrigin.Y;
            float dz = marker.PosZ - eyeOrigin.Z;
            float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

            if (dist > AimMaxDistance || dist < 1f)
                continue;

            float dot = (forward.X * dx + forward.Y * dy + forward.Z * dz) / dist;
            dot = Math.Clamp(dot, -1f, 1f);
            float angleDeg = MathF.Acos(dot) * (180f / MathF.PI);

            if (angleDeg < bestAngle)
            {
                bestAngle = angleDeg;
                best = marker;
            }
        }

        return best;
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

    private static float Distance(Vector a, float bx, float by, float bz)
    {
        float dx = a.X - bx;
        float dy = a.Y - by;
        float dz = a.Z - bz;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
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

        return $"<font color='#ffcc66'>&lt; {techniquePart} &gt;</font>{strengthPart}";
    }
}
