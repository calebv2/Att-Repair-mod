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
        Assert(RepairGlowPolicy.HaloParticleLifetimeSeconds > 0f,
            "The Repair Alloy aura particles must have a visible lifetime.");
        Assert(RepairGlowPolicy.HaloParticleSize > 0f,
            "The Repair Alloy aura particles must have a visible size.");
        Assert(RepairGlowPolicy.HaloParticleSize <= 0.06f,
            "The Repair Alloy aura must remain smaller than the previous oversized effect.");
        Assert(RepairGlowPolicy.HaloParticlePulseMaximumScale > RepairGlowPolicy.HaloParticlePulseMinimumScale,
            "The Repair Alloy aura must pulse between two distinct sizes.");
        Assert(RepairGlowPolicy.HaloShaderName == "Legacy Shaders/Particles/Additive",
            "The Repair Alloy aura must use the game's shipped additive particle shader.");
        Assert(RepairGlowPolicy.HaloTextureResolution >= 16,
            "The Repair Alloy halo must use a sufficiently smooth radial texture.");
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
}
