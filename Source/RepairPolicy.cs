namespace RepairHammer;

public static class RepairPolicy
{
    // The vanilla Small Hammer Mould casts the head, not the separate Hammer
    // item. ImpactTool retains this head pickup after it is attached to a
    // handle, so this is the identity checked during a repair strike.
    public const uint RepairHammerHeadItemHash = 14274u;
    public const float RepairFraction = 0.20f;

    public static bool CanRepair(bool isHot, bool isCompleted, float currentDurability, float maximumDurability)
    {
        return isHot && isCompleted && maximumDurability > 0f && currentDurability < maximumDurability;
    }

    public static float RestoreDurability(float currentDurability, float maximumDurability)
    {
        return System.Math.Min(maximumDurability, currentDurability + (maximumDurability * RepairFraction));
    }

    public static float ConsumeHammerDurability(float currentDurability, float maximumDurability)
    {
        return System.Math.Max(0f, currentDurability - (maximumDurability * RepairFraction));
    }

    public static bool CanRepairIntegrity(bool isHot, bool isCompleted, float integrity)
    {
        return isHot && isCompleted && integrity >= 0f && integrity < 1f;
    }

    public static float RestoreIntegrity(float integrity)
    {
        return RestoreIntegrity(integrity, RepairFraction);
    }

    public static float ConsumeHammerIntegrity(float integrity)
    {
        return ConsumeHammerIntegrity(integrity, RepairFraction);
    }

    public static float RestoreIntegrity(float integrity, float repairFraction)
    {
        return System.Math.Min(1f, integrity + repairFraction);
    }

    public static float ConsumeHammerIntegrity(float integrity, float hammerDamageFraction)
    {
        return System.Math.Max(0f, integrity - hammerDamageFraction);
    }
}
