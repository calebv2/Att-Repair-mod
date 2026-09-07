namespace RepairHammer;

public static class ForgeMultiplierPolicy
{
    public const float MinimumRepairFraction = 0.05f;
    public const float MaximumRepairFraction = 0.50f;
    public const float MinimumHammerDamageFraction = 0.02f;
    public const float MaximumHammerDamageFraction = 0.25f;

    public static bool ShouldReport(float forgeMultiplier)
    {
        return !float.IsNaN(forgeMultiplier)
            && !float.IsInfinity(forgeMultiplier)
            && forgeMultiplier > 0f;
    }

    public static bool TryCreateRepairProfile(
        float forgeMultiplier,
        float minimumMultiplier,
        float maximumMultiplier,
        out MaterialRepairProfile profile)
    {
        profile = default;
        if (!ShouldReport(forgeMultiplier)
            || !ShouldReport(minimumMultiplier)
            || !ShouldReport(maximumMultiplier)
            || maximumMultiplier <= minimumMultiplier)
        {
            return false;
        }

        var forgeEase = (forgeMultiplier - minimumMultiplier) / (maximumMultiplier - minimumMultiplier);
        forgeEase = System.Math.Max(0f, System.Math.Min(1f, forgeEase));
        var repairFraction = MinimumRepairFraction
            + (forgeEase * (MaximumRepairFraction - MinimumRepairFraction));
        var hammerDamageFraction = MaximumHammerDamageFraction
            - (forgeEase * (MaximumHammerDamageFraction - MinimumHammerDamageFraction));
        profile = new MaterialRepairProfile(repairFraction, hammerDamageFraction);
        return true;
    }
}
