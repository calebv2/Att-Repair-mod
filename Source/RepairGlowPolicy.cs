namespace RepairHammer;

public static class RepairGlowPolicy
{
    public static bool ShouldApply(bool isClient, bool isRepairAlloy)
    {
        return isClient && isRepairAlloy;
    }

    public static bool ShouldCreateParticleAura()
    {
        return false;
    }

    public static bool ShouldForceHeatedMaterialTemperature(bool isClient, bool isRepairAlloy)
    {
        return isClient && isRepairAlloy;
    }

    public static bool ShouldMaintainHeatedMaterialTemperature(bool isClient, bool isRepairAlloy)
    {
        return isClient && isRepairAlloy;
    }

    public static float SelectFullyHeatedTemperatureValue(float[] curveValues)
    {
        if (curveValues == null || curveValues.Length == 0)
        {
            throw new System.ArgumentException("The heat curve must contain at least one value.", nameof(curveValues));
        }

        return curveValues[curveValues.Length - 1];
    }

    public static bool ShouldInspectHeatComponents(bool isClient, bool isRepairAlloy)
    {
        return isClient && isRepairAlloy;
    }
}
