using CounterStrikeSharp.API.Core;

namespace NarcosSkinServer.Services;

public class WeaponService
{
    public void GiveSkeletonKnife(CCSPlayerController player)
    {
        if (!player.IsValid)
            return;

        if (!player.PlayerPawn.IsValid)
            return;

        // Don't remove anything yet.
        // First verify the correct classname.

        player.GiveNamedItem("weapon_knife");

        player.PrintToChat("[NarcosSkinServer] GiveNamedItem executed.");
    }
}