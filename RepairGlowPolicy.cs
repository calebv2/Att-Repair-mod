namespace RepairHammer;

public static class RepairGlowPolicy
{
    public const float HaloParticleLifetimeSeconds = 0.7f;
    public const float HaloParticleSize = 0.045f;
    public const float HaloParticlePulseMinimumScale = 0.35f;
    public const float HaloParticlePulseMaximumScale = 1f;
    public const string HaloShaderName = "Legacy Shaders/Particles/Additive";
    public const int HaloTextureResolution = 64;

    public static bool ShouldApply(bool isClient, bool isRepairAlloy)
    {
        return isClient && isRepairAlloy;
    }
}
