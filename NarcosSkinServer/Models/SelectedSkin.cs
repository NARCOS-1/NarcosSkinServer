namespace NarcosSkinServer.Models;

public class SelectedSkin
{
    public required WeaponDefinition Weapon { get; init; }

    public required int PaintKit { get; init; }

    public required float Wear { get; init; }

    public bool StatTrak { get; init; }

    public int Seed { get; init; } = 0;
}