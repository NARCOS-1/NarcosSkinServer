namespace NarcosSkinServer.Data;

public static class PaintKitNames
{
    public static readonly Dictionary<int, string> Names = new()
    {
        { 38, "Fade" },
        { 44, "Case Hardened" },
        { 618, "Ultraviolet" },
        { 10048, "Vice" },
        { 10073, "Slingshot" },
        { 10075, "Scarlet Shamagh" },
        { 10087, "Needle Point" }
    };

    public static string Get(int paint)
    {
        return Names.TryGetValue(paint, out var name)
            ? name
            : $"Paint {paint}";
    }
}