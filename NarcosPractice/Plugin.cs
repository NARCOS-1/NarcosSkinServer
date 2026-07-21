using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using NarcosPractice.Menus;
using NarcosPractice.Services;

namespace NarcosPractice;

public partial class Plugin : BasePlugin
{
    private LineupService? _lineupService;
    private PracticeService? _practiceService;

    public override string ModuleName => "NarcosPractice";
    public override string ModuleVersion => "0.1.0";
    public override string ModuleAuthor => "Zein";
    public override string ModuleDescription => "Nade lineup practice with insta-throw replay";

    public override void Load(bool hotReload)
    {
        string pluginDirectory = Path.GetDirectoryName(ModulePath)!;

        _lineupService = new LineupService(pluginDirectory);
        _practiceService = new PracticeService(_lineupService);

        RegisterListeners();

        AddCommand("css_nadesave", "Arm a lineup save - throw the nade right after", OnNadeSaveCommand);
        AddCommand("css_nades", "Open the nade practice menu", OnNadesCommand);
        AddCommand("css_nadedelete", "Delete a saved lineup", OnNadeDeleteCommand);

        Logger.LogInformation("NarcosPractice loaded!");
    }

    private void OnNadeSaveCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (command.ArgCount != 2)
        {
            player.PrintToChat("Usage: !nadesave <name> - then throw the nade immediately.");
            return;
        }

        _practiceService?.ArmSave(player, command.GetArg(1));
    }

    private void OnNadesCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (_lineupService == null || _practiceService == null)
            return;

        LineupMenu.Open(player, this, _lineupService, _practiceService, _currentMap);
    }

    private void OnNadeDeleteCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (command.ArgCount != 3)
        {
            player.PrintToChat("Usage: !nadedelete <name> <smoke/flash/he/molotov>");
            return;
        }

        if (!Enum.TryParse<Models.NadeType>(command.GetArg(2), true, out var type))
        {
            player.PrintToChat("Invalid nade type - use smoke, flash, he, or molotov.");
            return;
        }

        bool deleted = _lineupService?.DeleteLineup(_currentMap, command.GetArg(1), type) ?? false;
        player.PrintToChat(deleted
            ? $"[Practice] Deleted '{command.GetArg(1)}'."
            : $"[Practice] No lineup named '{command.GetArg(1)}' found.");
    }
}
