using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using NarcosPractice.Menus;
using NarcosPractice.Services;

namespace NarcosPractice;

public partial class Plugin : BasePlugin
{
    private MarkerService? _markerService;
    private MarkerVisualService? _markerVisualService;
    private PracticeService? _practiceService;

    public override string ModuleName => "NarcosPractice";
    public override string ModuleVersion => "0.3.0";
    public override string ModuleAuthor => "Zein";
    public override string ModuleDescription => "Nade lineup practice: visible markers, guided stand/aim, technique HUD, reset-and-retry, fly-and-verify";

    public override void Load(bool hotReload)
    {
        string pluginDirectory = Path.GetDirectoryName(ModulePath)!;

        _markerService = new MarkerService(pluginDirectory);
        _markerVisualService = new MarkerVisualService();
        _practiceService = new PracticeService(_markerService, _markerVisualService);

        RegisterListeners();

        // OnMapStart won't fire again for whatever map is already running when this
        // plugin loads (or hot-reloads), so seed the current map and spawn its
        // markers instead of waiting for the next map change. Server.MapName can
        // be null this early in Load() (before the engine's finished registering
        // the map), so this is deferred a second rather than read immediately -
        // reading it as null crashed plugin load entirely last time.
        AddTimer(1.0f, () =>
        {
            _currentMap = Server.MapName ?? _currentMap;
            RefreshMarkerVisuals();
        });

        AddCommand("css_nadesave", "Start saving a lineup here - pick type/technique/strength, then throw", OnNadeSaveCommand);
        AddCommand("css_nades", "Open the nade practice menu", OnNadesCommand);
        AddCommand("css_nadedelete", "Delete a saved lineup", OnNadeDeleteCommand);
        AddCommand("css_nadereset", "Instantly return to the last guided stand spot/angle/nade", OnNadeResetCommand);
        AddCommand("css_verify", "Toggle noclip to fly up and check where your throw landed", OnVerifyCommand);

        Logger.LogInformation("NarcosPractice loaded!");
    }

    private void OnNadeResetCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        _practiceService?.Reset(player);
    }

    private void OnVerifyCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        _practiceService?.ToggleVerify(player);
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

        if (deleted && marker != null)
        {
            // DeleteLineup already removed the marker from storage if that was its
            // last lineup - mirror that on the visual side too.
            if (marker.Lineups.Count == 0)
                _markerVisualService?.RemoveMarkerText(marker.Id);
            else
                _markerVisualService?.SpawnMarkerText(marker);
        }

        player.PrintToChat(deleted
            ? $"[Practice] Deleted '{command.GetArg(1)}'."
            : $"[Practice] No lineup named '{command.GetArg(1)}' found.");
    }
}
