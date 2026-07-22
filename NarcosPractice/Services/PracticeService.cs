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

    private record PendingSave(string Name, NadeType Type, ThrowTechnique Technique, ThrowStrength Strength, Vector ThrowPos, QAngle ThrowAngles, DateTime ArmedAt);

    private readonly ConcurrentDictionary<int, PendingSave> _pending = new();

    // Rising-edge tracking for the Use (E) button, per player slot.
    private readonly ConcurrentDictionary<int, bool> _usedLastTick = new();

    // Last lineup a player was guided to, so !nadereset can put them right back
    // without walking back or re-opening the menu.
    private readonly ConcurrentDictionary<int, Lineup> _lastGuided = new();

    private readonly ConcurrentDictionary<int, bool> _noclip = new();

    private readonly MarkerService _markerService;

    public PracticeService(MarkerService markerService)
    {
        _markerService = markerService;
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

        player.PrintToChat($"[Practice] Saved {type} lineup '{pending.Name}' to a marker here.");
    }

    // The core practice loop: teleport to the stand spot, face the saved aim angle,
    // give the correct (empty) nade so it's in hand, and tell you the technique/
    // strength to use. You throw it yourself - this only sets up the attempt.
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

        player.PrintToChat(goingNoclip
            ? "[Practice] Noclip on - fly up to check your throw, !verify again to drop back down."
            : "[Practice] Noclip off.");
    }

    // Drives the "walk up, press E" interaction. Called once per tick from Events.cs.
    public void Tick(CCSPlayerController player, string map, Action<Marker> onInteract)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            return;

        var pos = pawn.AbsOrigin;
        var marker = _markerService.FindNearest(map, pos.X, pos.Y, pos.Z, MarkerService.InteractRadius);

        bool usingNow = (player.Buttons & PlayerButtons.Use) != 0;
        bool usedLastTick = _usedLastTick.GetOrAdd(player.Slot, false);
        _usedLastTick[player.Slot] = usingNow;

        if (marker == null)
        {
            player.PrintToCenterHtml("");
            return;
        }

        int count = marker.Lineups.Count;
        player.PrintToCenterHtml($"<font color='#8fd3ff'>Press E</font> - {count} lineup{(count == 1 ? "" : "s")} saved here");

        if (usingNow && !usedLastTick)
            onInteract(marker);
    }
}
