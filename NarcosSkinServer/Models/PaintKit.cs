namespace NarcosSkinServer.Models;

public class PaintKit
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public List<int> Weapons { get; set; } = [];
}