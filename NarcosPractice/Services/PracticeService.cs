using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using NarcosPractice.Models;

namespace NarcosPractice.Services;

public class PracticeService
{
    // How fast a replayed nade travels - real collision/physics still apply, this
    // just compresses the normal ~700-900 u/s throw speed into something that lands
    // in a fraction of a second instead of making you wait out the real flight time.
    private const float InstaThrowSpeed = 6000f;

    private const float PendingSaveTimeoutSeconds = 10f;

    private record PendingSave(string Name, Vector ThrowPos, QAngle ThrowAngles, DateTime ArmedAt);

    private readonly ConcurrentDictionary<int, PendingSave> _pending = new();
    private readonly LineupService _lineupService;

    public PracticeService(LineupService lineupService)
    {
        _lineupService = lineupService;
    }

    public void ArmSave(CCSPlayerController player, string name)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null || pawn.EyeAngles == null)
            return;

        _pending[player.Slot] = new PendingSave(
            name,
            new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
            new QAngle(pawn.EyeAngles.X, pawn.EyeAngles.Y, pawn.EyeAngles.Z),
            DateTime.UtcNow);

        player.PrintToChat($"[Practice] Armed lineup save '{name}' - throw the nade now.");
    }

    // Called from the relevant *_detonate/inferno_startburn game event handler once
    // it's confirmed to belong to this player and this nade type.
    public void CompletePendingSave(CCSPlayerController player, string map, NadeType type, Vector detonatePos)
    {
        if (!_pending.TryRemove(player.Slot, out var pending))
            return;

        if ((DateTime.UtcNow - pending.ArmedAt).TotalSeconds > PendingSaveTimeoutSeconds)
        {
            player.PrintToChat("[Practice] Lineup save expired - run !nadesave again right before throwing.");
            return;
        }

        var lineup = new Lineup
        {
            Name = pending.Name,
            Type = type,
            ThrowPosX = pending.ThrowPos.X,
            ThrowPosY = pending.ThrowPos.Y,
            ThrowPosZ = pending.ThrowPos.Z,
            ThrowAngPitch = pending.ThrowAngles.X,
            ThrowAngYaw = pending.ThrowAngles.Y,
            DetonatePosX = detonatePos.X,
            DetonatePosY = detonatePos.Y,
            DetonatePosZ = detonatePos.Z
        };

        _lineupService.SaveLineup(map, lineup);
        player.PrintToChat($"[Practice] Saved {type} lineup '{pending.Name}'.");
    }

    public void Throw(CCSPlayerController player, Lineup lineup)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        var throwPos = new Vector(lineup.ThrowPosX, lineup.ThrowPosY, lineup.ThrowPosZ);
        var throwAngles = new QAngle(lineup.ThrowAngPitch, lineup.ThrowAngYaw, 0);

        pawn.Teleport(throwPos, throwAngles, new Vector(0, 0, 0));

        float dirX = lineup.DetonatePosX - throwPos.X;
        float dirY = lineup.DetonatePosY - throwPos.Y;
        float dirZ = lineup.DetonatePosZ - throwPos.Z;
        float length = MathF.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);

        if (length < 1f)
            length = 1f;

        var velocity = new Vector(
            dirX / length * InstaThrowSpeed,
            dirY / length * InstaThrowSpeed,
            dirZ / length * InstaThrowSpeed);

        Server.NextFrame(() => SpawnProjectile(lineup.Type, throwPos, velocity));
    }

    // Matches CHR15cs/CS2-Practice-Plugin's ProjectileManager approach: create the
    // concrete typed projectile, set its initial position/velocity, then spawn it -
    // rather than assume a shared base type exposes these fields identically.
    private static void SpawnProjectile(NadeType type, Vector position, Vector velocity)
    {
        switch (type)
        {
            case NadeType.Smoke:
                Spawn<CSmokeGrenadeProjectile>("smokegrenade_projectile", position, velocity);
                break;
            case NadeType.Flash:
                Spawn<CFlashbangProjectile>("flashbang_projectile", position, velocity);
                break;
            case NadeType.HE:
                Spawn<CHEGrenadeProjectile>("hegrenade_projectile", position, velocity);
                break;
            case NadeType.Molotov:
                Spawn<CMolotovProjectile>("molotov_projectile", position, velocity);
                break;
        }
    }

    private static void Spawn<T>(string designerName, Vector position, Vector velocity) where T : CBaseCSGrenadeProjectile
    {
        var projectile = Utilities.CreateEntityByName<T>(designerName);
        if (projectile == null)
            return;

        projectile.InitialPosition.X = position.X;
        projectile.InitialPosition.Y = position.Y;
        projectile.InitialPosition.Z = position.Z;

        projectile.InitialVelocity.X = velocity.X;
        projectile.InitialVelocity.Y = velocity.Y;
        projectile.InitialVelocity.Z = velocity.Z;

        projectile.DispatchSpawn();

        projectile.Teleport(position, null, velocity);
    }
}
