using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CS2MenuManager.API.Class;
using CS2MenuManager.API.Menu;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NarcosSkinServer.Data;
using NarcosSkinServer.Menus.Catagories;
using NarcosSkinServer.Models;
using NarcosSkinServer.Services;
using static CounterStrikeSharp.API.Core.Listeners;
using static NarcosSkinServer.Data.Variables;
using NarcosEconomy;

namespace NarcosSkinServer;

public partial class Plugin : BasePlugin
{

    
    private WeaponService? _weaponService;
    private EconomyService? _economyService;
    private InspectSessionService? _inspectSessionService;

    private int _KnifePaint = 38;
    private float _KnifeWear = 0.0001f;
    public override string ModuleName => "NarcosSkinServer";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "Zein";
    public override string ModuleDescription => "Private CS2 Skin Inspection Server";

    public override void Load(bool hotReload)
    {
        _weaponService = new WeaponService();
        _economyService = new EconomyService();
        _inspectSessionService = new InspectSessionService();

        string pluginDirectory = Path.GetDirectoryName(ModulePath)!;

        Logger.LogInformation($"Plugin directory: {pluginDirectory}");

        Dictionary<string, int> weaponDefIndexes = WeaponDefindex
            .GroupBy(kvp => kvp.Value)
            .ToDictionary(g => g.Key, g => g.First().Key);

        Economy.Initialize(pluginDirectory, weaponDefIndexes);

        // Per-skin legacy_model flags (whether a paint kit uses the pre-2023 mesh/UV
        // layout). GivePlayerWeaponSkin picks the weapon's bodygroup off this, and
        // legacy vs modern is genuinely per-skin - defaulting it one way or the other
        // when this list is empty renders some skins with the wrong mesh entirely.
        string skinLegacyModelsPath = Path.Combine(pluginDirectory, "Data", "SkinLegacyModels.json");

        if (File.Exists(skinLegacyModelsPath))
        {
            SkinsList = JArray.Parse(File.ReadAllText(skinLegacyModelsPath)).OfType<JObject>().ToList();
            Logger.LogInformation($"Loaded {SkinsList.Count} skin legacy-model entries.");
        }
        else
        {
            Logger.LogWarning($"SkinLegacyModels.json not found at {skinLegacyModelsPath}; legacy/modern weapon meshes may render incorrectly.");
        }

        // Hooks GiveNamedItemFunc + an OnEntitySpawned backstop to actually apply
        // stored weapon skins on give/spawn; was defined but never called.
        RegisterListeners();

        // NarcosSkinServer links its own copy of CS2MenuManager.dll (via HintPath),
        // isolated from the CS2MenuManager-MenuManager plugin's copy under CounterStrikeSharp's
        // per-plugin AssemblyLoadContext. That means this copy's static ConfigManager.Config
        // (WasdMenu FreezePlayer, colors, etc.) never gets loaded from shared/CS2MenuManager/config.toml
        // unless we reload it ourselves here. Fully qualified because CS2MenuManager.API.Class.MenuManager
        // and CounterStrikeSharp.API.Modules.Menu.MenuManager (also in scope) collide.
        CS2MenuManager.API.Class.MenuManager.ReloadConfig();

        Logger.LogInformation("NarcosSkinServer loaded!");

        AddCommand("css_narcos_test", "Tests NarcosSkinServer", OnTestCommand);
        AddCommand("css_skeleton", "Give Skeleton Knife", OnSkeletonCommand);

        AddCommand("css_skins", "Open skin browser", OnSkinsCommand);

        AddCommand("css_gloves", "Lists loaded gloves", OnGlovesCommand);
        AddCommand("css_knife", "Inspect a knife", OnKnifeCommand);
        AddCommand("css_weapon", "Apply a gun skin with an exact seed", OnWeaponCommand);
        AddCommand("css_menuscrollup", "internal: mouse wheel up", OnMenuScrollUp);
        AddCommand("css_menuscrolldown", "internal: mouse wheel down", OnMenuScrollDown);
        AddTimer(3.0f, () =>
        {
            // Set here instead of server.cfg because plugin/server updates
            // routinely overwrite server.cfg, and this way the environment is
            // always correct after any redeploy without a manual re-edit.

            // Cheats & networking
            Server.ExecuteCommand("sv_cheats 1");
            Server.ExecuteCommand("sv_lan 0");

            // Bots (none, this is a solo/inspection server)
            Server.ExecuteCommand("bot_quota 0");
            Server.ExecuteCommand("bot_kick");

            // Warmup: skip it entirely
            Server.ExecuteCommand("mp_warmup_end");
            Server.ExecuteCommand("mp_warmuptime 0");
            Server.ExecuteCommand("mp_do_warmup_offline 0");

            // Round/freeze time: no friction between spawning and playing
            Server.ExecuteCommand("mp_freezetime 0");
            Server.ExecuteCommand("mp_round_restart_delay 0");
            Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
            Server.ExecuteCommand("mp_roundtime 60");
            Server.ExecuteCommand("mp_roundtime_defuse 60");
            Server.ExecuteCommand("mp_roundtime_hostage 60");
            Server.ExecuteCommand("mp_maxrounds 0");
            Server.ExecuteCommand("mp_timelimit 0");

            // Buying: anywhere, anytime, full armor free
            Server.ExecuteCommand("mp_buy_anywhere 1");
            Server.ExecuteCommand("mp_buytime 9999");
            Server.ExecuteCommand("mp_free_armor 2");

            // Money: maxed out
            Server.ExecuteCommand("mp_startmoney 65535");
            Server.ExecuteCommand("mp_maxmoney 65535");
            Server.ExecuteCommand("mp_afterroundmoney 65535");

            // Ammo
            Server.ExecuteCommand("sv_infinite_ammo 2");

            // Survivability (closest vanilla approximation to "can't die")
            Server.ExecuteCommand("mp_respawn_on_death_ct 1");
            Server.ExecuteCommand("mp_respawn_on_death_t 1");
            Server.ExecuteCommand("mp_respawn_immunitytime 9999");

            // Team/balance friction removal
            Server.ExecuteCommand("mp_autoteambalance 0");
            Server.ExecuteCommand("mp_limitteams 0");
            Server.ExecuteCommand("mp_solid_teammates 0");

            // Misc QoL
            Server.ExecuteCommand("mp_drop_knife_enable 1");
            Server.ExecuteCommand("mp_death_drop_gun 0");
            Server.ExecuteCommand("mp_death_drop_grenade 0");
            Server.ExecuteCommand("mp_forcecamera 0");
            Server.ExecuteCommand("sv_talk_enemy_dead 1");
            Server.ExecuteCommand("sv_talk_enemy_living 1");

            // Fall damage has no equivalent "cancel it in code" hook - it's purely
            // cvar-driven, unlike bullet/blast damage which OnPlayerHurt corrects.
            Server.ExecuteCommand("sv_falldamage_scale 0");
        });
    }
    private void OnGlovesCommand(CCSPlayerController? player, CommandInfo command)
{
    if (player == null || !player.IsValid || player.IsBot)
        return;

    if (command.ArgCount != 5)
    {
        player.PrintToChat("Usage: !gloves <gloveId> <paint> <wear> <seed>");
        return;
    }

    if (!int.TryParse(command.GetArg(1), out int gloveId))
    {
        player.PrintToChat("Invalid glove ID.");
        return;
    }

    if (!int.TryParse(command.GetArg(2), out int paint))
    {
        player.PrintToChat("Invalid paint.");
        return;
    }

    if (!float.TryParse(command.GetArg(3), out float wear))
    {
        player.PrintToChat("Invalid wear.");
        return;
    }

    if (!int.TryParse(command.GetArg(4), out int seed))
    {
        player.PrintToChat("Invalid seed.");
        return;
    }

    var session = _inspectSessionService!.Get(player);

    session.Gloves ??= new WeaponInfo();

    session.Gloves.DefIndex = gloveId;
    session.Gloves.Paint = paint;
    session.Gloves.Wear = wear;
    session.Gloves.Seed = seed;

    _economyService!.ApplyInspectSession(player, session);

        player.PrintToChat($"[Narcos] {WeaponNames.Get(gloveId)} | {Economy.GetPaintKit(paint).Name ?? $"Paint {paint}"}");
    }
    


    private void OnKnifeCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (command.ArgCount != 5)
        {
            player.PrintToChat("Usage: !knife <knifeId> <paint> <wear> <seed>");
            return;
        }

        if (!int.TryParse(command.GetArg(1), out int knifeId))
        {
            player.PrintToChat("Invalid knife ID.");
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int paint))
        {
            player.PrintToChat("Invalid paint ID.");
            return;
        }

        if (!float.TryParse(command.GetArg(3), out float wear))
        {
            player.PrintToChat("Invalid wear.");
            return;
        }

        if (!int.TryParse(command.GetArg(4), out int seed))
        {
            player.PrintToChat("Invalid seed.");
            return;
        }

        if (!WeaponDefindex.TryGetValue(knifeId, out var knifeName))
        {
            player.PrintToChat("Invalid knife ID.");
            return;
        }

        var session = _inspectSessionService!.Get(player);

        session.KnifeName = knifeName;
        session.Knife ??= new WeaponInfo();

        session.Knife.Paint = paint;
        session.Knife.Wear = wear;
        session.Knife.Seed = seed;
        session.Knife.StatTrak = false;
        session.Knife.StatTrakCount = 0;
        session.Knife.Nametag = "";

        _economyService.ApplyInspectSession(player, session);

        Server.PrintToConsole(
    $"[Narcos] {WeaponNames.Get(knifeId)} | {Economy.GetPaintKit(paint).Name ?? $"Paint {paint}"}");

        player.PrintToChat($"[Narcos] {WeaponNames.Get(knifeId)} | {Economy.GetPaintKit(paint).Name ?? $"Paint {paint}"}");
    }

    // The WASD skin menu only offers a handful of fixed seed presets (0, 100, 250,
    // 500, 661, Random) since CS2MenuManager has no free-text input. This command
    // fills the gap for picking an exact seed/pattern (e.g. to match a specific
    // showcase image), the same way css_knife/css_gloves already do for those categories.
    private void OnWeaponCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (command.ArgCount != 5)
        {
            player.PrintToChat("Usage: !weapon <defindex> <paint> <wear> <seed>");
            return;
        }

        if (!int.TryParse(command.GetArg(1), out int defIndex))
        {
            player.PrintToChat("Invalid weapon defindex.");
            return;
        }

        if (!int.TryParse(command.GetArg(2), out int paint))
        {
            player.PrintToChat("Invalid paint ID.");
            return;
        }

        if (!float.TryParse(command.GetArg(3), out float wear))
        {
            player.PrintToChat("Invalid wear.");
            return;
        }

        if (!int.TryParse(command.GetArg(4), out int seed))
        {
            player.PrintToChat("Invalid seed.");
            return;
        }

        var weaponDef = SkinCatalog.Weapons.Values.FirstOrDefault(w => w.DefIndex == defIndex);

        if (weaponDef == null)
        {
            player.PrintToChat("Invalid weapon defindex.");
            return;
        }

        _economyService!.ApplySkin(player, weaponDef, paint, wear, seed);

        player.PrintToChat($"[Narcos] {weaponDef.Name} | {Economy.GetPaintKit(paint).Name ?? $"Paint {paint}"} | wear {wear} | seed {seed}");
    }

    // Bound to the mouse wheel (see Events.cs OnPlayerSpawn). Scrolls the active
    // WASD menu if one's open; otherwise falls back to the game's normal
    // next/previous weapon switch so we don't break scroll-to-switch in regular play.
    private void OnMenuScrollUp(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (CS2MenuManager.API.Class.MenuManager.GetActiveMenu(player) is WasdMenuInstance menu)
            menu.ScrollUp();
        else
            player.ExecuteClientCommand("invprev");
    }

    private void OnMenuScrollDown(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (CS2MenuManager.API.Class.MenuManager.GetActiveMenu(player) is WasdMenuInstance menu)
            menu.ScrollDown();
        else
            player.ExecuteClientCommand("invnext");
    }


    private void OnSkinsCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        player.PrintToChat("[Narcos] OnSkinsCommand");
        player.PrintToChat("[Narcos] MainMenu.Open");

        player.PrintToChat("[Narcos] Before MainMenu.Open");

        MainMenu.Open(player, this, _economyService!);

        player.PrintToChat("[Narcos] After MainMenu.Open");
    }


    public override void Unload(bool hotReload)
    {
        Logger.LogInformation("NarcosSkinServer unloaded.");
    }
    
    private void OnTestCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        var session = _inspectSessionService!.Get(player);
        _economyService?.ApplyInspectSession(player, session);

        player.PrintToChat("[NarcosSkinServer] Applying Skeleton Fade...");
    }

    private void OnSkeletonCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null)
        {
            Logger.LogInformation("css_skeleton executed from server console.");
            return;
        }

        _weaponService?.GiveSkeletonKnife(player);
    }

}