using CounterStrikeSharp.API.Core;

namespace NarcosSkinServer.Models;

public class WeaponDefinition
{
    public string Name { get; set; } = "";

    public int DefIndex { get; set; }

    public WeaponCategory Category { get; set; }
}