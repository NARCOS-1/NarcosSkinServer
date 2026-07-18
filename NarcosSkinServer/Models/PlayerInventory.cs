using System.Collections.Generic;

namespace NarcosSkinServer.Models;

public class PlayerInventory
{
    public string Knife { get; set; } = "weapon_knife";

    public Dictionary<string, WeaponInfo> Weapons { get; set; } = new();
}