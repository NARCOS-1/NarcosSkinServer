using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using NarcosSkinServer.Data;
using NarcosSkinServer.Models;
using NarcosSkinServer.Models;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using System;
using System.Collections.Concurrent;
using static NarcosSkinServer.Data.Variables;
namespace NarcosSkinServer.Services;


public class EconomyService
{

    public string KnifeName { get; set; } = "weapon_knife_skeleton";

    public WeaponInfo Knife { get; } = new()
    {
        Paint = 38,
        Wear = 0.0001f,
        Seed = 0,
        StatTrak = false,
        StatTrakCount = 0,
        Nametag = "",
        Stickers = new(),
        KeyChain = null
    };
    private static readonly MemoryFunctionVoid<nint, string, float>
    CAttributeListSetOrAddAttributeValueByName =
        new(GameData.GetSignature("CAttributeList_SetOrAddAttributeValueByName"));

    private const ulong MinimumCustomItemId = 65578;

    private ulong _nextItemId = MinimumCustomItemId;

    private int _fadeSeed = 1;

    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, float>>
        _temporaryPlayerWeaponWear = new();

    public void GivePlayerWeaponSkin(CCSPlayerController player, CBasePlayerWeapon weapon)
    {
   
        if (!GPlayerWeaponsInfo.TryGetValue(player.Slot, out _)) return;

        bool isKnife = weapon.DesignerName.Contains("knife") || weapon.DesignerName.Contains("bayonet");

        switch (isKnife)
        {
            case true when !HasChangedKnife(player, out var _):
                return;

            case true:
                {
                    Server.PrintToConsole($"GivePlayerWeaponSkin: {GPlayersKnife[player.Slot][player.Team]}");
                    var newDefIndex = WeaponDefindex.FirstOrDefault(x => x.Value == GPlayersKnife[player.Slot][player.Team]);
                    if (newDefIndex.Key == 0) return;

                    

                    if (weapon.AttributeManager.Item.ItemDefinitionIndex != newDefIndex.Key)
                    {
                        

                        SubclassChange(weapon, (ushort)newDefIndex.Key);

                        
                    }

                    weapon.AttributeManager.Item.ItemDefinitionIndex = (ushort)newDefIndex.Key;
                    
                    weapon.AttributeManager.Item.EntityQuality = 3;

                    weapon.AttributeManager.Item.AttributeList.Attributes.RemoveAll();
                    weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();
                    break;
                }
            default:
               weapon.AttributeManager.Item.EntityQuality = 0;
                break;
        }

        UpdatePlayerEconItemId(weapon.AttributeManager.Item);

        int weaponDefIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
        int fallbackPaintKit;

        weapon.AttributeManager.Item.AccountID = (uint)player.SteamID;

        List<JObject> skinInfo;
        bool isLegacyModel;

        if (!HasChangedPaint(player, weaponDefIndex, out _))
        {
            // Random skins
            weapon.FallbackPaintKit = GetRandomPaint(weaponDefIndex);
            weapon.FallbackSeed = 0;
            weapon.FallbackWear = 0.01f;

            weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture prefab", GetRandomPaint(weaponDefIndex));
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture seed", 0);
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture wear", 0.01f);

            weapon.AttributeManager.Item.AttributeList.Attributes.RemoveAll();
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.AttributeList.Handle, "set item texture prefab", GetRandomPaint(weaponDefIndex));
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.AttributeList.Handle, "set item texture seed", 0);
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.AttributeList.Handle, "set item texture wear", 0.01f);

            fallbackPaintKit = weapon.FallbackPaintKit;

            if (fallbackPaintKit == 0)
                return;

            skinInfo = SkinsList
                .Where(w =>
                    w["weapon_defindex"]?.ToObject<int>() == weaponDefIndex &&
                    w["paint"]?.ToObject<int>() == fallbackPaintKit)
                .ToList();

            isLegacyModel = skinInfo.Count <= 0 || skinInfo[0].Value<bool>("legacy_model");
            UpdatePlayerWeaponMeshGroupMask(player, weapon, isLegacyModel);
            return;
        }

        if (!HasChangedPaint(player, weaponDefIndex, out var weaponInfo) || weaponInfo == null)
            return;
        
        //Log($"Apply on {weapon.DesignerName}({weapon.AttributeManager.Item.ItemDefinitionIndex}) paint {gPlayerWeaponPaints[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} seed {gPlayerWeaponSeed[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]} wear {gPlayerWeaponWear[steamId.SteamId64][weapon.AttributeManager.Item.ItemDefinitionIndex]}");

        weapon.AttributeManager.Item.AttributeList.Attributes.RemoveAll();
        weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();

        UpdatePlayerEconItemId(weapon.AttributeManager.Item);

        weapon.AttributeManager.Item.CustomName = weaponInfo.Nametag;
        weapon.FallbackPaintKit = weaponInfo.Paint;
        

        weapon.FallbackSeed = weaponInfo is { Paint: 38, Seed: 0 } ? _fadeSeed++ : weaponInfo.Seed;

        weapon.FallbackWear = weaponInfo.Wear;
        CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "set item texture prefab", weapon.FallbackPaintKit);

        if (weaponInfo.StatTrak)
        {
            weapon.AttributeManager.Item.EntityQuality = 9;

            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "kill eater", ViewAsFloat((uint)weaponInfo.StatTrakCount));
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle, "kill eater score type", 0);
            
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.AttributeList.Handle, "kill eater", ViewAsFloat((uint)weaponInfo.StatTrakCount));
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.AttributeList.Handle, "kill eater score type", 0);
            
        }

        fallbackPaintKit = weapon.FallbackPaintKit;

        if (fallbackPaintKit == 0)
            return;

        if (weaponInfo.KeyChain != null) SetKeychain(player, weapon);
        if (weaponInfo.Stickers.Count > 0) SetStickers(player, weapon);

        skinInfo = SkinsList
            .Where(w =>
                w["weapon_defindex"]?.ToObject<int>() == weaponDefIndex &&
                w["paint"]?.ToObject<int>() == fallbackPaintKit)
            .ToList();

        isLegacyModel = skinInfo.Count <= 0 || skinInfo[0].Value<bool>("legacy_model");
        UpdatePlayerWeaponMeshGroupMask(player, weapon, isLegacyModel);
    }


    private void IncrementWearForWeaponWithStickers(CCSPlayerController player, CBasePlayerWeapon weapon)
    {
        int weaponDefIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;
        if (!HasChangedPaint(player, weaponDefIndex, out var weaponInfo) || weaponInfo == null ||
            weaponInfo.Stickers.Count <= 0) return;

        float wearIncrement = 0.001f;
        float currentWear = weaponInfo.Wear;

        var playerWear = _temporaryPlayerWeaponWear.GetOrAdd(player.Slot, _ => new ConcurrentDictionary<int, float>());

        float incrementedWear = playerWear.AddOrUpdate(
            weaponDefIndex,
            currentWear + wearIncrement,
            (_, oldWear) => Math.Min(oldWear + wearIncrement, 1.0f)
        );

        weapon.FallbackWear = incrementedWear;
    }


    private void SetStickers(CCSPlayerController? player, CBasePlayerWeapon weapon)
    {
        if (player == null || !player.IsValid) return;

        int weaponDefIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;

        if (!HasChangedPaint(player, weaponDefIndex, out var weaponInfo) || weaponInfo == null)
            return;

        foreach (var sticker in weaponInfo.Stickers)
        {
            int stickerSlot = weaponInfo.Stickers.IndexOf(sticker);

            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
                $"sticker slot {stickerSlot} id", ViewAsFloat(sticker.Id));
            if (sticker.OffsetX != 0 || sticker.OffsetY != 0)
                CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
                    $"sticker slot {stickerSlot} schema", 0);
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
                $"sticker slot {stickerSlot} offset x", sticker.OffsetX);
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
                $"sticker slot {stickerSlot} offset y", sticker.OffsetY);
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
                $"sticker slot {stickerSlot} wear", sticker.Wear);
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
                $"sticker slot {stickerSlot} scale", sticker.Scale);
            CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
                $"sticker slot {stickerSlot} rotation", sticker.Rotation);
        }

        if (_temporaryPlayerWeaponWear.TryGetValue(player.Slot, out var playerWear) &&
            playerWear.TryGetValue(weaponDefIndex, out float storedWear))
        {
            weapon.FallbackWear = storedWear;
        }
    }

    private void SetKeychain(CCSPlayerController? player, CBasePlayerWeapon weapon)
    {
        if (player == null || !player.IsValid) return;

        int weaponDefIndex = weapon.AttributeManager.Item.ItemDefinitionIndex;

        if (!HasChangedPaint(player, weaponDefIndex, out var value) || value?.KeyChain == null)
            return;

        var keyChain = value.KeyChain;

        CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
            "keychain slot 0 id", ViewAsFloat(keyChain.Id));
        CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
            "keychain slot 0 offset x", keyChain.OffsetX);
        CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
            "keychain slot 0 offset y", keyChain.OffsetY);
        CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
            "keychain slot 0 offset z", keyChain.OffsetZ);
        CAttributeListSetOrAddAttributeValueByName.Invoke(weapon.AttributeManager.Item.NetworkedDynamicAttributes.Handle,
            "keychain slot 0 seed", ViewAsFloat(keyChain.Seed));
    }

    private static void GiveKnifeToPlayer(CCSPlayerController? player)
    {
        if (player == null || !player.IsValid)
            return;

        if (PlayerHasKnife(player)) return;

        //string knifeToGive = (CsTeam)player.TeamNum == CsTeam.Terrorist ? "weapon_knife_t" : "weapon_knife";
        player.GiveNamedItem(CsItem.Knife);
        Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
    }

    private static bool PlayerHasKnife(CCSPlayerController? player)
    {
       

        if (player == null || !player.IsValid || !player.PlayerPawn.IsValid)
        {
            return false;
        }

        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.WeaponServices == null || player.PlayerPawn.Value.ItemServices == null)
            return false;

        var weapons = player.PlayerPawn.Value.WeaponServices?.MyWeapons;
        if (weapons == null) return false;
        foreach (var weapon in weapons)
        {
            if (!weapon.IsValid || weapon.Value == null || !weapon.Value.IsValid) continue;
            if (weapon.Value.DesignerName.Contains("knife") || weapon.Value.DesignerName.Contains("bayonet"))
            {
                return true;
            }
        }
        return false;
    }
    private static bool _gBCommandsAllowed = true;
    private void RefreshKnife(CCSPlayerController player)
    {
        var weapons = player.PlayerPawn?.Value?.WeaponServices?.MyWeapons;

        if (weapons != null)
        {
            foreach (var handle in weapons)
            {
                if (!handle.IsValid || handle.Value == null || !handle.Value.IsValid)
                    continue;

                if (handle.Value.DesignerName.Contains("knife") ||
                    handle.Value.DesignerName.Contains("bayonet"))
                {
                    handle.Value.AddEntityIOEvent(
                        "Kill",
                        handle.Value,
                        null,
                        "",
                        0.0f);

                    break;
                }
            }
        }
        var newKnife = new CBasePlayerWeapon(player.GiveNamedItem(CsItem.Knife));
            GivePlayerWeaponSkin(player, newKnife);

            var newWeapon = new CBasePlayerWeapon(player.GiveNamedItem(CsItem.USP));
        // player.GiveNamedItem(CsItem.Knife);
        // player.ExecuteClientCommand("slot3");

        Server.NextFrame(() =>
        {
            try
            {
                // Don't kill the knife.

                if (newWeapon != null && newWeapon.IsValid)
                    newWeapon.AddEntityIOEvent("Kill", newWeapon, null, "", 0.01f);
            }
            catch (Exception)
            {
            }
        });
    }
    private void GivePlayerGloves(CCSPlayerController player)
    {
        if (player == null ||
     !player.IsValid ||
     !player.PawnIsAlive)
         {
             return;
         }

         CCSPlayerPawn? pawn = player.PlayerPawn.Value;
         if (pawn == null || !pawn.IsValid)
             return;

         CEconItemView item = pawn.EconGloves;

         item.NetworkedDynamicAttributes.Attributes.RemoveAll();
         item.AttributeList.Attributes.RemoveAll();

         //force gloves model refresh to prevent model overlap
         player.ExecuteClientCommand("lastinv");
         Server.NextFrame(() =>
         {

             try
             {
                 if (!player.IsValid)
                     return;

                 if (!player.PawnIsAlive)
                     return;

                 if (!GPlayersGlove.TryGetValue(player.Slot, out var gloveInfo) ||
                    !gloveInfo.TryGetValue(player.Team, out var gloveId) ||
                     gloveId == 0)
                 {
                     return;
                 }

                 if (!HasChangedPaint(player, gloveId, out var weaponInfo))
                 {
                     return;
                 }

                 if (weaponInfo == null)
                 {
                     return;
                 }

                 item.ItemDefinitionIndex = (ushort)gloveId;

                 UpdatePlayerEconItemId(item);

                 item.NetworkedDynamicAttributes.Attributes.RemoveAll();
                 CAttributeListSetOrAddAttributeValueByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture prefab", weaponInfo.Paint);
                 CAttributeListSetOrAddAttributeValueByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture seed", weaponInfo.Seed);
                 CAttributeListSetOrAddAttributeValueByName.Invoke(item.NetworkedDynamicAttributes.Handle, "set item texture wear", weaponInfo.Wear);

                 item.AttributeList.Attributes.RemoveAll();
                 CAttributeListSetOrAddAttributeValueByName.Invoke(item.AttributeList.Handle, "set item texture prefab", weaponInfo.Paint);
                 CAttributeListSetOrAddAttributeValueByName.Invoke(item.AttributeList.Handle, "set item texture seed", weaponInfo.Seed);
                 CAttributeListSetOrAddAttributeValueByName.Invoke(item.AttributeList.Handle, "set item texture wear", weaponInfo.Wear);

                 item.Initialized = true;

                 //force gloves model refresh to prevent model overlap
                 player.ExecuteClientCommand("lastinv");
                 SetBodygroup(pawn, "first_or_third_person", 0);
                 Server.NextFrame(() =>
                 {
                     SetBodygroup(pawn, "first_or_third_person", 1);
                 });
             }
             catch (Exception) { }
         });

    }

    public void RefreshWeapons(CCSPlayerController? player)
    {
        

        if (!_gBCommandsAllowed)
        {
            
            return;
        }

        if (player == null || !player.IsValid || player.PlayerPawn.Value == null ||
            (LifeState_t)player.LifeState != LifeState_t.LIFE_ALIVE)
        {
            
            return;
        }

        if (player.PlayerPawn.Value.WeaponServices == null ||
            player.PlayerPawn.Value.ItemServices == null)
        {
            
            return;
        }

        var weapons = player.PlayerPawn.Value.WeaponServices.MyWeapons;

        if (weapons.Count == 0)
        {
            
            return;
        }

        ;

        if (player.Team is CsTeam.None or CsTeam.Spectator)
        {
            
            return;
        }

        var hasKnife = false;

        Dictionary<string, List<(int, int)>> weaponsWithAmmo = [];

        foreach (var weapon in weapons)
        {
            

            if (!weapon.IsValid)
            {
                
                continue;
            }

            if (weapon.Value == null)
            {
                
                continue;
            }

            if (!weapon.Value.IsValid)
            {
                
                continue;
            }

           

            if (!weapon.Value.DesignerName.Contains("weapon_"))
            {
                
                continue;
            }

            CCSWeaponBaseGun gun = weapon.Value.As<CCSWeaponBaseGun>();

                if (weapon.Value.Entity == null) continue;
                if (!weapon.Value.OwnerEntity.IsValid) continue;
                if (gun.Entity == null) continue;
                if (!gun.IsValid) continue;

                try
                {
                    CCSWeaponBaseVData? weaponData = weapon.Value.As<CCSWeaponBase>().VData;

                    if (weaponData == null) continue;

                    if (weaponData.GearSlot is gear_slot_t.GEAR_SLOT_RIFLE or gear_slot_t.GEAR_SLOT_PISTOL)
                    {
                        if (!WeaponDefindex.TryGetValue(weapon.Value.AttributeManager.Item.ItemDefinitionIndex, out var weaponByDefindex))
                            continue;

                        int clip1 = weapon.Value.Clip1;
                        int reservedAmmo = weapon.Value.ReserveAmmo[0];

                        if (!weaponsWithAmmo.TryGetValue(weaponByDefindex, out var value))
                        {
                            value = [];
                            weaponsWithAmmo.Add(weaponByDefindex, value);
                        }

                        value.Add((clip1, reservedAmmo));

                        if (gun.VData == null) return;

                        weapon.Value?.AddEntityIOEvent("Kill", weapon.Value, null, "", 0.1f);
                    }
                

                if (weaponData.GearSlot == gear_slot_t.GEAR_SLOT_KNIFE)
                    {
                        weapon.Value?.AddEntityIOEvent("Kill", weapon.Value, null, "", 0.1f);
                        hasKnife = true;
                    }
                }
                catch (Exception ex)
                {
                    
                }
            }

            Server.NextFrame(() =>
            {
                if (!_gBCommandsAllowed)
                {
                    
                    return;
                }

                

                

                foreach (var entry in weaponsWithAmmo)
                {
                    foreach (var ammo in entry.Value)
                    {
                        var newWeapon = new CBasePlayerWeapon(player.GiveNamedItem(entry.Key));

                        Server.NextFrame(() =>
                        {
                            try
                            {
                                newWeapon.Clip1 = ammo.Item1;
                                newWeapon.ReserveAmmo[0] = ammo.Item2;

                                GivePlayerWeaponSkin(player, newWeapon);
                                
                                IncrementWearForWeaponWithStickers(player, newWeapon);
                            }
                            catch (Exception ex)
                            {
                                
                            }
                        });
                    }
                }
            });
        }

    private static int GetRandomPaint(int defindex)
    {
        if (SkinsList.Count == 0)
            return 0;

        Random rnd = new Random();

        // Filter weapons by the provided defindex
        var filteredWeapons = SkinsList.Where(w => w["weapon_defindex"]?.ToString() == defindex.ToString()).ToList();

        if (filteredWeapons.Count == 0)
            return 0;

        var randomWeapon = filteredWeapons[rnd.Next(filteredWeapons.Count)];

        return int.TryParse(randomWeapon["paint"]?.ToString(), out var paintValue) ? paintValue : 0;
    }

    public static void SubclassChange(CBasePlayerWeapon weapon, ushort itemD)
    {
        weapon.AcceptInput("ChangeSubclass", value: itemD.ToString());
    }

    public static void SetBodygroup(CCSPlayerPawn pawn, string group, int value)
    {
        pawn.AcceptInput("SetBodygroup", value: $"{group},{value}");
    }

    private void UpdateWeaponMeshGroupMask(CBaseEntity weapon, bool isLegacy = false)
    {
        if (weapon.CBodyComponent?.SceneNode == null) return;
        //var skeleton = weapon.CBodyComponent.SceneNode.GetSkeletonInstance();
        // skeleton.ModelState.MeshGroupMask = isLegacy ? 2UL : 1UL;

        weapon.AcceptInput("SetBodygroup", value: $"body,{(isLegacy ? 1 : 0)}");
    }

    private void UpdatePlayerWeaponMeshGroupMask(CCSPlayerController player, CBasePlayerWeapon weapon, bool isLegacy)
    {
        UpdateWeaponMeshGroupMask(weapon, isLegacy);
    }

    private void GiveOnItemPickup(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        var myWeapons = pawn.WeaponServices?.MyWeapons;
        if (myWeapons == null) return;

        foreach (var handle in myWeapons)
        {
            var weapon = handle.Value;

            if (weapon == null || !weapon.IsValid) continue;
            if (myWeapons.Count == 1)
            {
                var newWeapon = new CBasePlayerWeapon(player.GiveNamedItem(CsItem.USP));
                weapon.AddEntityIOEvent("Kill", weapon, null, "", 0.01f);
                player.GiveNamedItem(CsItem.Knife);
                player.ExecuteClientCommand("slot3");
                newWeapon.AddEntityIOEvent("Kill", newWeapon, null, "", 0.01f);
            }

            GivePlayerWeaponSkin(player, weapon);
        }
    }

    private void UpdatePlayerEconItemId(CEconItemView econItemView)
    {
        var itemId = _nextItemId++;

        econItemView.ItemID = itemId;

        econItemView.ItemIDLow = (uint)itemId & 0xFFFFFFFF;
        econItemView.ItemIDHigh = (uint)itemId >> 32;
    }

    private static CCSPlayerController? GetPlayerFromItemServices(CCSPlayer_ItemServices itemServices)
    {
        var pawn = itemServices.Pawn.Value;
        if (!pawn.IsValid || !pawn.Controller.IsValid || pawn.Controller.Value == null) return null;
        var player = new CCSPlayerController(pawn.Controller.Value.Handle);
        return player.IsValid ? player : null;
    }

    private static bool HasChangedKnife(CCSPlayerController player, out string? knifeValue)
    {
        knifeValue = null;

        // Check if player has knife info for their slot and team
        if (!GPlayersKnife.TryGetValue(player.Slot, out var knife) ||
            !knife.TryGetValue(player.Team, out var value) ||
            value == "weapon_knife") return false;
        knifeValue = value; // Assign the knife value to the out parameter
        return true;
    }

    private static bool HasChangedPaint(CCSPlayerController player, int weaponDefIndex, out WeaponInfo? weaponInfo)
    {
        weaponInfo = null;

        // Check if player has weapons info for their slot and team
        if (!GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teamInfo) ||
            !teamInfo.TryGetValue(player.Team, out var teamWeapons))
        {
            return false;
        }

        // Check if the specified weapon has a paint/skin change
        if (!teamWeapons.TryGetValue(weaponDefIndex, out var value) || value.Paint <= 0) return false;

        weaponInfo = value; // Assign the out variable when it exists
        return true;
    }

    private static int GetKnifeDefinitionIndex(string knifeName)
    {
        var knife = WeaponDefindex.FirstOrDefault(x => x.Value == knifeName);

        if (knife.Key == 0)
            throw new Exception($"Unknown knife '{knifeName}'.");

        return knife.Key;
    }

    private static float ViewAsFloat(uint value)
    {
        return BitConverter.Int32BitsToSingle((int)value);
    }
    public void ApplyKnifeInspect(CCSPlayerController player, InspectSession session)
    {
        
        if (!GPlayersKnife.TryGetValue(player.Slot, out var knifeTeams))
        {
            knifeTeams = new();
            GPlayersKnife[player.Slot] = knifeTeams;
        }
        Server.PrintToConsole($"ApplyKnifeInspect: KnifeName={session.KnifeName}");

        knifeTeams[player.Team] = session.KnifeName;

        Server.PrintToConsole($"Stored GPlayersKnife={knifeTeams[player.Team]}");


        knifeTeams[player.Team] = session.KnifeName;

        if (!GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teams))
        {
            teams = new();
            GPlayerWeaponsInfo[player.Slot] = teams;
        }

        if (!teams.TryGetValue(player.Team, out var weapons))
        {
            weapons = new();
            teams[player.Team] = weapons;
        }

        weapons[GetKnifeDefinitionIndex(session.KnifeName)] = session.Knife!;

        RefreshKnife(player);
    }
    public void ApplyGloveInspect(CCSPlayerController player, InspectSession session)
    {
        if (session.Gloves == null)
            return;

        if (!GPlayersGlove.TryGetValue(player.Slot, out var gloveTeams))
        {
            gloveTeams = new();
            GPlayersGlove[player.Slot] = gloveTeams;
        }

        gloveTeams[player.Team] = session.Gloves.DefIndex;

        if (!GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teams))
        {
            teams = new();
            GPlayerWeaponsInfo[player.Slot] = teams;
        }

        if (!teams.TryGetValue(player.Team, out var weapons))
        {
            weapons = new();
            teams[player.Team] = weapons;
        }

        weapons[session.Gloves.DefIndex] = session.Gloves;

        GivePlayerGloves(player);
    }
    public void ApplyInspectSession(CCSPlayerController player, InspectSession session)
    {
        if (session.Knife != null)
        {
            ApplyKnifeInspect(player, session);
        }

        if (session.Gloves != null)
        {
            ApplyGloveInspect(player, session);
        }
    }
    public void ApplySkin(CCSPlayerController player, WeaponDefinition weapon, int paintKit, float wear, int seed)
    {
        if (!GPlayersKnife.TryGetValue(player.Slot, out var knifeTeams))
        {
            knifeTeams = new();
            GPlayersKnife[player.Slot] = knifeTeams;
        }

        knifeTeams[player.Team] = WeaponDefindex[weapon.DefIndex];

        if (!GPlayerWeaponsInfo.TryGetValue(player.Slot, out var teams))
        {
            teams = new();
            GPlayerWeaponsInfo[player.Slot] = teams;
        }

        if (!teams.TryGetValue(player.Team, out var weapons))
        {
            weapons = new();
            teams[player.Team] = weapons;
        }

        weapons[weapon.DefIndex] = new WeaponInfo
        {
            Paint = paintKit,
            Wear = wear,
            Seed = seed,
            StatTrak = false,
            StatTrakCount = 0,
            Nametag = "",
            Stickers = new(),
            KeyChain = null
        };

        RefreshKnife(player);
    }
}



