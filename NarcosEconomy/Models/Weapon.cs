namespace NarcosEconomy.Models;

public sealed class Weapon
{
    public required int DefIndex { get; init; }

    public required string InternalName { get; init; }

    public required string Name { get; init; }

    public required IReadOnlyList<PaintKit> PaintKits { get; init; }
}