using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using NarcosSkinServer.Services;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace NarcosSkinServer;

public partial class Plugin
{
    private void RegisterListeners()
    {
        VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPost, HookMode.Post);
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

        if (weapon.DesignerName.Contains("knife") ||
            weapon.DesignerName.Contains("bayonet"))
        {
            _economyService?.GivePlayerWeaponSkin(player, weapon);
        }

        return HookResult.Continue;

        
    }
}