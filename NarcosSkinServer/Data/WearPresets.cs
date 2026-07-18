namespace NarcosSkinServer.Data;

public static class WearPresets
{
    public static readonly (string Name, float Wear)[] All =
    {
        ("Factory New", 0.00f),
        ("Minimal Wear", 0.07f),
        ("Field-Tested", 0.15f),
        ("Well-Worn", 0.38f),
        ("Battle-Scarred", 0.45f)
    };
}