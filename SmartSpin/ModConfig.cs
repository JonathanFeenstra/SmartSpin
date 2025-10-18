namespace SmartSpin;

public sealed class ModConfig
{
    // See StardewValley.Menus.WheelSpinGame ctor: out of 30 possible initial velocities, 22 land on green (11 / 15) and 8 on orange (4 / 15)
    public float GreenProbability { get; set; } = 11f / 15f;
    public bool EnableLuckySpeedups { get; set; } = true;
    public float KellyFractionMultiplier { get; set; } = 1.0f;
}