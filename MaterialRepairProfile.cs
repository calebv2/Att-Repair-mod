namespace RepairHammer;

public readonly struct MaterialRepairProfile
{
    public MaterialRepairProfile(float repairFraction, float hammerDamageFraction)
    {
        RepairFraction = repairFraction;
        HammerDamageFraction = hammerDamageFraction;
    }

    public float RepairFraction { get; }
    public float HammerDamageFraction { get; }
}
