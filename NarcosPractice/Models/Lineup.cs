namespace NarcosPractice.Models;

public class Lineup
{
    public string Name { get; set; } = "";

    public NadeType Type { get; set; }

    public ThrowTechnique Technique { get; set; } = ThrowTechnique.Normal;

    public ThrowStrength Strength { get; set; } = ThrowStrength.Full;

    public float ThrowPosX { get; set; }
    public float ThrowPosY { get; set; }
    public float ThrowPosZ { get; set; }

    public float ThrowAngPitch { get; set; }
    public float ThrowAngYaw { get; set; }

    public float DetonatePosX { get; set; }
    public float DetonatePosY { get; set; }
    public float DetonatePosZ { get; set; }

    // The real authored "aim your crosshair here" point, when the source data
    // has one (seed data from real annotations does; !nadesave-recorded
    // lineups don't, since there's no separate aim_target concept there).
    // Null means "fall back to projecting a generic point along the angle."
    public float? AimPosX { get; set; }
    public float? AimPosY { get; set; }
    public float? AimPosZ { get; set; }

    // The original author's real freeform instruction (e.g. "Middle click",
    // "Running JumpThrow") - shown alongside our guessed Technique/Strength so
    // players have ground truth even where the guess is wrong.
    public string? Notes { get; set; }
}
