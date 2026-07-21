namespace NarcosPractice.Models;

// A physical spot in the world you walk up to and press E on. One marker can hold
// several lineups (e.g. multiple different smokes all thrown from this exact stance),
// selected and stacked through via the E-interact flow.
public class Marker
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }

    public List<Lineup> Lineups { get; set; } = [];
}
