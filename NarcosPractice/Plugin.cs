using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using NarcosPractice.Menus;
using NarcosPractice.Services;

namespace NarcosPractice;

public partial class Plugin : BasePlugin
{
    private MarkerService? _markerService;
    private PracticeService? _practiceService;

    public override string ModuleName => "NarcosPractice";
    public override string ModuleVersion => "0.2.0";
    public override string ModuleAuthor => "Zein";
    public override string ModuleDescription => "Nade lineup practice with markers and insta-throw replay";

    public override void Load(bool hotReload)
    {
        string pluginDirectory = Path.GetDirectoryName(ModulePath)!;

        _markerService = new MarkerService(pluginDirectory);
        _practiceService = new PracticeService(_markerService);

        RegisterListeners();

        AddCommand("css_nadesave", "Start saving a lineup here - pick type/technique/strength, then throw", OnNadeSaveCommand);
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
            player.PrintToChat("Usage: !nadesave <name> - pick type/technique/strength, then throw immediately.");
            return;
        }

        if (_practiceService != null)
            SaveWizardMenu.Open(player, this, _practiceService, command.GetArg(1));
    }

    private void OnNadesCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        if (_markerService == null || _practiceService == null)
            return;

        LineupMenu.Open(player, this, _markerService, _practiceService, _currentMap);
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

        var marker = _markerService?.GetMarkers(_currentMap)
            .FirstOrDefault(m => m.Lineups.Any(l => l.Name.Equals(command.GetArg(1), StringComparison.OrdinalIgnoreCase) && l.Type == type));

        bool deleted = marker != null && (_markerService?.DeleteLineup(_currentMap, marker, command.GetArg(1), type) ?? false);
        player.PrintToChat(deleted
            ? $"[Practice] Deleted '{command.GetArg(1)}'."
            : $"[Practice] No lineup named '{command.GetArg(1)}' found.");
    }
}
