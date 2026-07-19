using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CS2MenuManager.API.Menu;
using Microsoft.Extensions.Logging;
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

        Economy.Initialize(pluginDirectory);



        Logger.LogInformation("NarcosSkinServer loaded!");

        AddCommand("css_narcos_test", "Tests NarcosSkinServer", OnTestCommand);
        AddCommand("css_skeleton", "Give Skeleton Knife", OnSkeletonCommand);

        AddCommand("css_skins", "Open skin browser", OnSkinsCommand);

        AddCommand("css_gloves", "Lists loaded gloves", OnGlovesCommand);
        AddCommand("css_knife", "Inspect a knife", OnKnifeCommand);
        AddTimer(3.0f, () =>
        {
            // Warmup
            Server.ExecuteCommand("mp_warmup_end");

            // Bots
            Server.ExecuteCommand("bot_kick");
            Server.ExecuteCommand("bot_quota 0");

            // Money
            Server.ExecuteCommand("mp_startmoney 65535");
            Server.ExecuteCommand("mp_maxmoney 65535");
            Server.ExecuteCommand("mp_afterroundmoney 65535");

            // Buying
            Server.ExecuteCommand("mp_buy_anywhere 1");
            Server.ExecuteCommand("mp_buytime 9999");

            // Gameplay
            Server.ExecuteCommand("mp_freezetime 0");
            Server.ExecuteCommand("sv_cheats 1");

            Server.ExecuteCommand("mp_ignore_round_win_conditions 1");
            
            Server.ExecuteCommand("sv_infinite_ammo 2");

            Server.ExecuteCommand("mp_roundtime_defuse 60");
            Server.ExecuteCommand("mp_roundtime_hostage 60");
            Server.ExecuteCommand("mp_roundtime 60");

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