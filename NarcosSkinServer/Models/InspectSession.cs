using System.Collections.Generic;

namespace NarcosSkinServer.Models;

public class InspectSession
{
    public string KnifeName { get; set; } = "weapon_knife";

    public WeaponInfo? Knife { get; set; }

    public Dictionary<int, WeaponInfo> Weapons { get; } = new();

    public WeaponInfo? Gloves { get; set; }

}