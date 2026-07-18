namespace NarcosEconomy.Models;

public sealed class PaintKit
{
    public required int Id { get; init; }

    public required string Weapon { get; init; }

    public required string InternalName { get; init; }

    public required string Name { get; init; }

    public required string Rarity { get; init; }

    public required float MinFloat { get; init; }

    public required float MaxFloat { get; init; }
}