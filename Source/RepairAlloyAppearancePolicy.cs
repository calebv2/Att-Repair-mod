using System;

namespace RepairHammer;

public static class RepairAlloyAppearancePolicy
{
    public const string TemplateMaterialName = "Iron";
    public const float IceTintRed = 0f;
    public const float IceTintGreen = 210f / 255f;
    public const float IceTintBlue = 1f;
    public const float IceTintAlpha = 0.8f;
    public const float IceEmissionRed = 185f / 255f;
    public const float IceEmissionGreen = 1f;
    public const float IceEmissionBlue = 254f / 255f;
    public static bool IsExpectedTemplateMaterial(string materialName)
    {
        return string.Equals(materialName, TemplateMaterialName, StringComparison.Ordinal);
    }

    public static bool ShouldTintShaderProperty(string propertyName)
    {
        return propertyName == "_ColorA"
            || propertyName == "_ColorB"
            || propertyName == "_Color";
    }

    public static bool ShouldSetEmissionShaderProperty(string propertyName)
    {
        return propertyName == "_Emission"
            || propertyName == "_EmissionColor";
    }

    public static bool ShouldInspectSurfaceShaderProperty(string propertyName)
    {
        return propertyName == "_MetallicStrength"
            || propertyName == "_Glossiness";
    }
}
