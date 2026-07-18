namespace NarcosSkinServer.Models;

public class SkinDefinition
{
    public int PaintKit { get; set; }

    public float Wear { get; set; }

    public int Seed { get; set; }

    public bool StatTrak { get; set; }

    public int StatTrakCount { get; set; }

    public string NameTag { get; set; } = "";
}