namespace NarcosPractice.Models;

public class Lineup
{
    public string Name { get; set; } = "";

    public NadeType Type { get; set; }

    public float ThrowPosX { get; set; }
    public float ThrowPosY { get; set; }
    public float ThrowPosZ { get; set; }

    public float ThrowAngPitch { get; set; }
    public float ThrowAngYaw { get; set; }

    public float DetonatePosX { get; set; }
    public float DetonatePosY { get; set; }
    public float DetonatePosZ { get; set; }
}
