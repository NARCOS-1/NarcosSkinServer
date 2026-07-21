using CounterStrikeSharp.API.Core;
using CS2MenuManager.API.Menu;
using NarcosPractice.Models;
using NarcosPractice.Services;

namespace NarcosPractice.Menus;

// Chains three quick picks (type -> technique -> strength) before arming a lineup
// save, so a saved lineup carries the tags needed to browse/filter it later.
public static class SaveWizardMenu
{
    public static void Open(CCSPlayerController player, BasePlugin plugin, PracticeService practiceService, string name)
    {
        var menu = new WasdMenu("Nade Type?", plugin);

        foreach (NadeType type in Enum.GetValues<NadeType>())
        {
            menu.AddItem(type.ToString(), (p, o) =>
            {
                OpenTechnique(p, plugin, practiceService, name, type, menu);
            });
        }

        menu.Display(player, 0);
    }

    private static void OpenTechnique(CCSPlayerController player, BasePlugin plugin, PracticeService practiceService, string name, NadeType type, WasdMenu previousMenu)
    {
        var menu = new WasdMenu("Throw Technique?", plugin)
        {
            PrevMenu = previousMenu
        };

        foreach (ThrowTechnique technique in Enum.GetValues<ThrowTechnique>())
        {
            menu.AddItem(technique.ToString(), (p, o) =>
            {
                OpenStrength(p, plugin, practiceService, name, type, technique, menu);
            });
        }

        menu.Display(player, 0);
    }

    private static void OpenStrength(CCSPlayerController player, BasePlugin plugin, PracticeService practiceService, string name, NadeType type, ThrowTechnique technique, WasdMenu previousMenu)
    {
        var menu = new WasdMenu("Throw Strength?", plugin)
        {
            PrevMenu = previousMenu
        };

        foreach (ThrowStrength strength in Enum.GetValues<ThrowStrength>())
        {
            menu.AddItem(strength.ToString(), (p, o) =>
            {
                practiceService.ArmSave(p, name, type, technique, strength);
            });
        }

        menu.Display(player, 0);
    }
}
