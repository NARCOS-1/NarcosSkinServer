using NarcosSkinServer.Models;

namespace NarcosSkinServer.Models;

public static class TestInventoryFactory
{
    public static PlayerInventory CreateSkeletonFade()
    {
        var inventory = new PlayerInventory
        {
            Knife = "weapon_knife_skeleton"
        };

        inventory.Weapons["weapon_knife_skeleton"] = new WeaponInfo
        {
            Paint = 38,
            Seed = 0,
            Wear = 0.0001f,
            StatTrak = false,
            StatTrakCount = 0,
            Nametag = "",
            Stickers = [],
            KeyChain = null
        };

        return inventory;
    }
}