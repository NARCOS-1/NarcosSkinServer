using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using NarcosSkinServer.Data;
using NarcosSkinServer.Services;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using static CounterStrikeSharp.API.Core.Listeners;

namespace NarcosSkinServer;

public partial class Plugin
{
    // sv_cheats has to stay on (it's what lets us set weapon paint attributes),
    // but that also unlocks these engine cheat commands for every connected
    // player, not just us. Block them for anyone without @css/cheats instead of
    // relying on admins.json alone, since admins.json only gates things a plugin
    // explicitly checks - it does nothing to raw engine console commands by itself.
    private static readonly string[] BlockedCheatCommands =
    [
        "noclip",
        "god",
        "buddha",
        "notarget",
        "give",
        "impulse",
        "sv_cheats",
        "sv_infinite_ammo",
        "sv_gravity",
        "host_timescale",
        "map",
        "changelevel",
        "kick",
        "banid",
        "rcon",
    ];

    private void RegisterListeners()
    {
        VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPost, HookMode.Post);
        RegisterListener<OnEntitySpawned>(OnEntityCreated);
        AddCommandListener("say", OnPlayerSay);
        AddCommandListener("say_team", OnPlayerSay);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        RegisterListener<OnMapStart>(OnMapStart);

        foreach (string command in BlockedCheatCommands)
            AddCommandListener(command, OnCheatCommandAttempt);
    }

    // Source 2 refuses SetModel() on anything not already in the map's precache list -
    // "resource ... requested is not loaded and may have been deleted" is that check
    // failing, not necessarily a bad path. Agent models aren't part of any map's default
    // precache, so we have to register them ourselves on every map load.
    private void OnMapStart(string mapName)
    {
        foreach (var agent in AgentCatalog.Agents)
        {
            // A bad path here must not be allowed to take out map-start registration for
            // everything else in the plugin (commands, listeners, etc.) - it did exactly
            // that when this loop had no guard.
            try
            {
                Server.PrecacheModel(agent.ModelPath);
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[Narcos] Failed to precache agent model '{agent.ModelPath}' ({agent.Name}): {ex.Message}");
            }
        }
    }

    private HookResult OnCheatCommandAttempt(CCSPlayerController? player, CommandInfo command)
    {
        // Only restrict actual clients; the server console (player == null) is us.
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (AdminManager.PlayerHasPermissions(player, "@css/cheats"))
            return HookResult.Continue;

        player.PrintToChat($"[Narcos] '{command.GetArg(0)}' is restricted to admins.");
        return HookResult.Stop;
    }

    // Mouse wheel isn't a PlayerButtons flag CS2MenuManager's WasdMenu can read like
    // W/S, so we bind it client-side to our own commands (css_menuscrollup/down),
    // which scroll the active menu when one's open and otherwise fall back to the
    // game's normal weapon-switch behavior.
    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        player.ExecuteClientCommand("bind \"MWHEELUP\" \"css_menuscrollup\"");
        player.ExecuteClientCommand("bind \"MWHEELDOWN\" \"css_menuscrolldown\"");

        // Player models reset to the team default on every respawn.
        _economyService?.ReapplyAgent(player);

        return HookResult.Continue;
    }

    // sv_falldamage_scale 0 handles fall damage at the cvar level, but bullets/blasts/
    // grenades still deal damage through the normal combat path - this is the code-side
    // half: whatever damage just landed, immediately top health back up so it never
    // results in a kill. mp_respawn_immunitytime/mp_respawn_on_death_* still cover the
    // rare case something slips through (e.g. world damage, falling out of the map).
    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot || !player.PawnIsAlive)
            return HookResult.Continue;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null || !pawn.IsValid)
            return HookResult.Continue;

        pawn.Health = pawn.MaxHealth;
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

        if (pawn.ArmorValue < 100)
        {
            pawn.ArmorValue = 100;
            Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_ArmorValue");
        }

        return HookResult.Continue;
    }

    // Captures the "Custom Seed" chat input queued by SeedMenu/GloveSeedMenu, since
    // CS2MenuManager's WASD menus have no free-text input for typing an exact seed.
    private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        string message = command.GetArg(1).Trim();

        if (Variables.PendingWeaponSeedInput.TryRemove(player.Slot, out var weaponPending))
        {
            if (int.TryParse(message, out int seed) && seed >= 0)
            {
                _economyService?.ApplySkin(player, weaponPending.Weapon, weaponPending.PaintKit, weaponPending.Wear, seed);
                player.PrintToChat($"[Narcos] Applied seed {seed}.");
            }
            else
            {
                player.PrintToChat("[Narcos] Invalid seed - open the menu again and pick Custom Seed to retry.");
            }

            return HookResult.Stop;
        }

        if (Variables.PendingGloveSeedInput.TryRemove(player.Slot, out var glovePending))
        {
            if (int.TryParse(message, out int seed) && seed >= 0)
            {
                _economyService?.ApplyGlove(player, glovePending.Glove.DefIndex, glovePending.PaintKit, glovePending.Wear, seed);
                player.PrintToChat($"[Narcos] Applied seed {seed}.");
            }
            else
            {
                player.PrintToChat("[Narcos] Invalid seed - open the menu again and pick Custom Seed to retry.");
            }

            return HookResult.Stop;
        }

        return HookResult.Continue;
    }

    private static CCSPlayerController? GetPlayerFromItemServices(CCSPlayer_ItemServices itemServices)
    {
        var pawn = itemServices.Pawn.Value;
        if (!pawn.IsValid || !pawn.Controller.IsValid || pawn.Controller.Value == null)
            return null;

        var player = new CCSPlayerController(pawn.Controller.Value.Handle);

        return player == null || !player.IsValid || player.IsBot ? null : player;
    }

    private HookResult OnGiveNamedItemPost(DynamicHook hook)
    {
        var itemServices = hook.GetParam<CCSPlayer_ItemServices>(0);
        var weapon = hook.GetReturn<CBasePlayerWeapon>();

        var player = GetPlayerFromItemServices(itemServices);

        if (player == null || weapon == null || !weapon.IsValid)
            return HookResult.Continue;

        if (weapon.DesignerName.Contains("weapon"))
        {
            _economyService?.GivePlayerWeaponSkin(player, weapon);
        }

        return HookResult.Continue;
    }

    // Backstop for OnGiveNamedItemPost: some weapon give paths (e.g. subclass-swapped
    // rifles) don't reliably have their skin stick when painted only at give-time.
    // Re-apply once the entity is fully spawned, mirroring the pattern used by
    // Nereziel/cs2-WeaponPaints (a mature reference implementation this economy code
    // closely follows).
    private void OnEntityCreated(CEntityInstance entity)
    {
        if (!entity.DesignerName.Contains("weapon"))
            return;

        Server.NextWorldUpdate(() =>
        {
            try
            {
                var weapon = new CBasePlayerWeapon(entity.Handle);
                if (!weapon.IsValid)
                    return;

                if (!weapon.OwnerEntity.IsValid)
                    return;

                var player = Utilities.GetPlayerFromIndex((int)weapon.OwnerEntity.Index);

                if (player == null || !player.IsValid || player.IsBot)
                    return;

                _economyService?.GivePlayerWeaponSkin(player, weapon);
            }
            catch (Exception)
            {
            }
        });
    }
}
