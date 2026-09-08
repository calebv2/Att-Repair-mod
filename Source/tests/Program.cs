using System;
using RepairHammer;

internal static class Program
{
    private static void Main()
    {
        Assert(
            RepairAlloyPolicy.IsRepairAlloyRecipe(
                RepairPolicy.RepairHammerHeadItemHash,
                15,
                5),
            "Fifteen Crystal Gem Blue and five Gold ingots with the Small Hammer Mould must qualify as the repair-hammer recipe.");

        Assert(
            RepairAlloyPolicy.IsRepairAlloyRecipe(
                RepairPolicy.RepairHammerHeadItemHash,
                30,
                10),
            "Larger Crystal Gem Blue and Gold stacks must qualify so the smelter can consume fifteen crystals and five gold per recipe cycle.");

        Assert(
            !RepairAlloyPolicy.IsRepairAlloyRecipe(
                RepairPolicy.RepairHammerHeadItemHash,
                14,
                5),
            "Fourteen Crystal Gem Blue must not qualify for the fifteen-crystal repair-hammer recipe.");

        Assert(
            !RepairAlloyPolicy.IsRepairAlloyRecipe(
                RepairPolicy.RepairHammerHeadItemHash,
                15,
                4),
            "Four Gold ingots must not qualify for the five-gold repair-hammer recipe.");

        AssertForgeProfile(1.5f, 0.2f, 1.5f, 0.50f, 0.02f,
            "The easiest forgeable material must restore 50% while costing the repair hammer 2%.");
        AssertForgeProfile(0.2f, 0.2f, 1.5f, 0.05f, 0.25f,
            "The hardest forgeable material must restore 5% while costing the repair hammer 25%.");
        AssertForgeProfile(1f, 0.2f, 1.5f, 0.32692308f, 0.10846154f,
            "A middle forge multiplier must derive its repair values from its relative forge ease.");
        Assert(!ForgeMultiplierPolicy.TryCreateRepairProfile(0f, 0.2f, 1.5f, out _),
            "A material without a valid forge multiplier must not be repaired.");
        Assert(RepairGlowPolicy.ShouldApply(isClient: true, isRepairAlloy: true), "Repair Alloy must receive the client-side glow.");
        Assert(!RepairGlowPolicy.ShouldApply(isClient: false, isRepairAlloy: true), "Dedicated servers must not create visual glow objects.");
        Assert(!RepairGlowPolicy.ShouldApply(isClient: true, isRepairAlloy: false), "Ordinary hammer heads must not glow.");
        Assert(!RepairGlowPolicy.ShouldCreateParticleAura(),
            "Repair Alloy must not create a particle aura on the hammer head.");
        Assert(RepairGlowPolicy.ShouldForceHeatedMaterialTemperature(isClient: true, isRepairAlloy: true),
            "Repair Alloy must use the game's heated-material path on clients.");
        Assert(!RepairGlowPolicy.ShouldForceHeatedMaterialTemperature(isClient: false, isRepairAlloy: true),
            "Dedicated servers must not force heated-material visuals.");
        Assert(!RepairGlowPolicy.ShouldForceHeatedMaterialTemperature(isClient: true, isRepairAlloy: false),
            "Ordinary metal must not receive forced heated-material visuals.");
        Assert(RepairGlowPolicy.ShouldMaintainHeatedMaterialTemperature(isClient: true, isRepairAlloy: true),
            "Repair Alloy must maintain its heated-material visual after setup on clients.");
        Assert(!RepairGlowPolicy.ShouldMaintainHeatedMaterialTemperature(isClient: false, isRepairAlloy: true),
            "Dedicated servers must not maintain heated-material visuals.");
        Assert(!RepairGlowPolicy.ShouldMaintainHeatedMaterialTemperature(isClient: true, isRepairAlloy: false),
            "Ordinary metal must not maintain heated-material visuals.");
        AssertClose(RepairGlowPolicy.SelectFullyHeatedTemperatureValue(new[] { 0f, 0.25f, 1f }), 1f,
            "Repair Alloy must use the final value from the game's heat curve.");
        Assert(RepairGlowPolicy.ShouldInspectHeatComponents(isClient: true, isRepairAlloy: true),
            "Repair Alloy heat diagnostics must run on clients.");
        Assert(!RepairGlowPolicy.ShouldInspectHeatComponents(isClient: false, isRepairAlloy: true),
            "Repair Alloy heat diagnostics must not run on dedicated servers.");
        Assert(!RepairGlowPolicy.ShouldInspectHeatComponents(isClient: true, isRepairAlloy: false),
            "Ordinary materials must not produce Repair Alloy heat diagnostics.");
        Assert(RepairAlloyAppearancePolicy.IsExpectedTemplateMaterial("Iron"),
            "Repair Alloy must clone the Iron material appearance.");
        Assert(!RepairAlloyAppearancePolicy.IsExpectedTemplateMaterial("Silver"),
            "Repair Alloy must no longer clone the Silver material appearance.");
        Assert(RepairAlloyAppearancePolicy.ShouldTintShaderProperty("_ColorA"),
            "Repair Alloy must tint the Custom/Metal primary colour.");
        Assert(RepairAlloyAppearancePolicy.ShouldTintShaderProperty("_Color"),
            "Repair Alloy must tint the SimpleStandard primary colour.");
        Assert(!RepairAlloyAppearancePolicy.ShouldTintShaderProperty("_MainTex"),
            "Repair Alloy must preserve Iron's texture maps.");
        Assert(RepairAlloyAppearancePolicy.ShouldSetEmissionShaderProperty("_Emission"),
            "Repair Alloy must set Custom/Metal emission.");
        Assert(RepairAlloyAppearancePolicy.ShouldSetEmissionShaderProperty("_EmissionColor"),
            "Repair Alloy must set SimpleStandard emission.");
        Assert(RepairAlloyAppearancePolicy.ShouldInspectSurfaceShaderProperty("_MetallicStrength"),
            "Repair Alloy diagnostics must inspect the custom-metal metallic-strength property.");
        Assert(RepairAlloyAppearancePolicy.ShouldInspectSurfaceShaderProperty("_Glossiness"),
            "Repair Alloy diagnostics must inspect the glossiness property.");
        Assert(!RepairAlloyAppearancePolicy.ShouldInspectSurfaceShaderProperty("_Color"),
            "Repair Alloy diagnostics must only inspect the requested surface properties.");
        AssertClose(RepairAlloyAppearancePolicy.IceTintRed, 0f,
            "Repair Alloy ice tint red must be 0.");
        AssertClose(RepairAlloyAppearancePolicy.IceTintGreen, 210f / 255f,
            "Repair Alloy ice tint green must be 210/255.");
        AssertClose(RepairAlloyAppearancePolicy.IceTintBlue, 1f,
            "Repair Alloy ice tint blue must be 255/255.");
        AssertClose(RepairAlloyAppearancePolicy.IceTintAlpha, 0.8f,
            "Repair Alloy ice tint alpha must be 0.8.");
        AssertClose(RepairAlloyAppearancePolicy.IceEmissionRed, 185f / 255f,
            "Repair Alloy emission red must be 185/255.");
        AssertClose(RepairAlloyAppearancePolicy.IceEmissionGreen, 1f,
            "Repair Alloy emission green must be 255/255.");
        AssertClose(RepairAlloyAppearancePolicy.IceEmissionBlue, 254f / 255f,
            "Repair Alloy emission blue must be 254/255.");
        Assert(ForgeMultiplierPolicy.ShouldReport(1f),
            "Positive forge multipliers must be included in the startup report.");
        Assert(!ForgeMultiplierPolicy.ShouldReport(0f),
            "Zero forge multipliers must be excluded from the startup report.");
        Assert(SmelterInputFilterPolicy.ShouldAddItem(new uint[] { 17090u }, 45754u),
            "Crystal Gem Blue must be added when the smelter input filter does not already contain it.");
        Assert(!SmelterInputFilterPolicy.ShouldAddItem(new uint[] { 17090u, 45754u }, 45754u),
            "Crystal Gem Blue must not be added twice to the smelter input filter.");

        Console.WriteLine("PASS: repair hammer crystal-gem recipe policy.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertForgeProfile(
        float multiplier,
        float minimumMultiplier,
        float maximumMultiplier,
        float repairFraction,
        float hammerDamageFraction,
        string message)
    {
        Assert(ForgeMultiplierPolicy.TryCreateRepairProfile(
            multiplier,
            minimumMultiplier,
            maximumMultiplier,
            out var profile), message);
        Assert(Math.Abs(profile.RepairFraction - repairFraction) < 0.0001f, message);
        Assert(Math.Abs(profile.HammerDamageFraction - hammerDamageFraction) < 0.0001f, message);
    }

    private static void AssertClose(float actual, float expected, string message)
    {
        Assert(Math.Abs(actual - expected) < 0.0001f, message);
    }
}
