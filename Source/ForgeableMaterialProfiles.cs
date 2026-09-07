using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alta;
using Alta.Blacksmithing;
using Alta.Inventory;

namespace RepairHammer;

internal static class ForgeableMaterialProfiles
{
    private static readonly Dictionary<uint, MaterialRepairProfile> Profiles = new Dictionary<uint, MaterialRepairProfile>();

    public static void Initialize()
    {
        Profiles.Clear();
        PhysicalMaterial.CheckItems();
        MouldDefinition.CheckItems();

        var allowedMaterialsField = typeof(MouldDefinition).GetField("allowedMaterials", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(MouldDefinition).FullName, "allowedMaterials");
        var forgeMultiplierField = typeof(PhysicalMaterial).GetField("forgeMultiplier", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(PhysicalMaterial).FullName, "forgeMultiplier");

        var mouldDefinitions = MouldDefinition.All.ToArray();
        Core.Logger.Msg("Forge profile discovery: mouldDefinitions=" + mouldDefinitions.Length
            + ", allowedMaterialsFieldType=" + allowedMaterialsField.FieldType.FullName + ".");
        var forgeableMaterials = new Dictionary<uint, PhysicalMaterial>();
        foreach (var mould in mouldDefinitions)
        {
            var allowedMaterialsValue = allowedMaterialsField.GetValue(mould);
            if (allowedMaterialsValue is not ItemSet allowedMaterialSet)
            {
                if (mould.Hash == mouldDefinitions.FirstOrDefault()?.Hash)
                {
                    Core.Logger.Msg("Forge profile discovery: first allowedMaterials value type="
                        + (allowedMaterialsValue == null ? "<null>" : allowedMaterialsValue.GetType().FullName) + ".");
                }
                continue;
            }

            foreach (var allowedItem in allowedMaterialSet.Items)
            {
                var material = allowedItem?.Components.OfType<Ingot>().FirstOrDefault()?.PhysicalMaterial;
                if (material != null)
                {
                    forgeableMaterials[material.Hash] = material;
                }
            }
        }

        Core.Logger.Msg("Forge profile discovery: resolved " + forgeableMaterials.Count
            + " mould-allowed ingot materials.");

        var materialsWithMultiplier = forgeableMaterials.Values
            .Select(material => new
            {
                Material = material,
                ForgeMultiplier = Convert.ToSingle(forgeMultiplierField.GetValue(material))
            })
            .Where(entry => ForgeMultiplierPolicy.ShouldReport(entry.ForgeMultiplier))
            .ToArray();
        if (materialsWithMultiplier.Length < 2)
        {
            throw new InvalidOperationException("Forge repair profiles require at least two mould-allowed materials with positive forge multipliers.");
        }

        var minimumMultiplier = materialsWithMultiplier.Min(entry => entry.ForgeMultiplier);
        var maximumMultiplier = materialsWithMultiplier.Max(entry => entry.ForgeMultiplier);
        foreach (var entry in materialsWithMultiplier.OrderByDescending(entry => entry.ForgeMultiplier).ThenBy(entry => entry.Material.name, StringComparer.Ordinal))
        {
            if (!ForgeMultiplierPolicy.TryCreateRepairProfile(
                    entry.ForgeMultiplier,
                    minimumMultiplier,
                    maximumMultiplier,
                    out var profile))
            {
                continue;
            }

            Profiles[entry.Material.Hash] = profile;
            Core.Logger.Msg("Forge repair profile: material='" + entry.Material.name
                + "', multiplier=" + entry.ForgeMultiplier.ToString("F3")
                + ", repair=" + profile.RepairFraction.ToString("P0")
                + ", hammerDamage=" + profile.HammerDamageFraction.ToString("P0") + ".");
        }
    }

    public static bool TryGet(PhysicalMaterial material, out MaterialRepairProfile profile)
    {
        profile = default;
        return material != null && Profiles.TryGetValue(material.Hash, out profile);
    }
}
