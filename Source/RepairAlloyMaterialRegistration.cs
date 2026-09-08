using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alta;
using UnityEngine;

namespace RepairHammer;

public static class RepairAlloyMaterialRegistration
{
    private const string RepairAlloyMaterialName = "Repair Alloy";
    private static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(typeof(object).FullName, "MemberwiseClone");

    public static PhysicalMaterial? RepairAlloy { get; private set; }

    /// <summary>
    /// Clones the vanilla Iron physical material at runtime, then replaces its
    /// shared renderer materials with Repair-Alloy-only ice-blue copies.
    /// </summary>
    public static PhysicalMaterial CreateAndRegisterRuntimeClone()
    {
        if (RepairAlloy != null)
        {
            return RepairAlloy;
        }

        PhysicalMaterial.CheckItems();
        var template = PhysicalMaterial.All.FirstOrDefault(
            material => RepairAlloyAppearancePolicy.IsExpectedTemplateMaterial(material.name));
        if (template == null)
        {
            throw new InvalidOperationException(
                "Repair Alloy cannot be created because the vanilla '" + RepairAlloyAppearancePolicy.TemplateMaterialName
                + "' PhysicalMaterial template is unavailable in this game version.");
        }

        var repairAlloy = UnityEngine.Object.Instantiate(template);
        repairAlloy.name = RepairAlloyMaterialName;
        CloneAndConfigureAppearanceMaterials(repairAlloy);
        AssignStableHash(repairAlloy, RepairAlloyPolicy.RepairAlloyMaterialHash, RepairAlloyMaterialName);
        Register(repairAlloy);
        return repairAlloy;
    }

    private static void CloneAndConfigureAppearanceMaterials(PhysicalMaterial material)
    {
        var materialClones = new Dictionary<Material, Material>();
        foreach (var field in typeof(PhysicalMaterial).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.FieldType == typeof(Material) && field.GetValue(material) is Material sourceMaterial)
            {
                field.SetValue(material, GetOrCreateMaterialClone(sourceMaterial, materialClones));
            }
        }

        var channelsField = typeof(PhysicalMaterial).GetField("materialChannels", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(PhysicalMaterial).FullName, "materialChannels");
        if (channelsField.GetValue(material) is not Array channels)
        {
            throw new InvalidOperationException("Repair Alloy requires PhysicalMaterial.materialChannels to be an array.");
        }

        var clonedChannels = Array.CreateInstance(channels.GetType().GetElementType() ?? throw new InvalidOperationException("Repair Alloy material channel element type is unavailable."), channels.Length);
        for (var index = 0; index < channels.Length; index++)
        {
            var channel = channels.GetValue(index);
            if (channel == null)
            {
                continue;
            }

            var clonedChannel = MemberwiseCloneMethod.Invoke(channel, null)
                ?? throw new InvalidOperationException("Repair Alloy failed to clone an Iron material channel.");
            foreach (var field in channel.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType == typeof(Material) && field.GetValue(clonedChannel) is Material sourceMaterial)
                {
                    field.SetValue(clonedChannel, GetOrCreateMaterialClone(sourceMaterial, materialClones));
                }
            }

            clonedChannels.SetValue(clonedChannel, index);
        }

        channelsField.SetValue(material, clonedChannels);
        Core.Logger.Msg("Repair Alloy appearance: template='Iron', clonedMaterials=" + materialClones.Count + ", tint=ice-blue, emission=ice-blue.");
    }

    private static Material GetOrCreateMaterialClone(Material sourceMaterial, Dictionary<Material, Material> materialClones)
    {
        if (materialClones.TryGetValue(sourceMaterial, out var existingClone))
        {
            return existingClone;
        }

        var clone = new Material(sourceMaterial)
        {
            name = "Repair Alloy Ice " + sourceMaterial.name
        };
        var iceTint = new Color(
            RepairAlloyAppearancePolicy.IceTintRed,
            RepairAlloyAppearancePolicy.IceTintGreen,
            RepairAlloyAppearancePolicy.IceTintBlue,
            RepairAlloyAppearancePolicy.IceTintAlpha);
        var iceEmission = new Color(
            RepairAlloyAppearancePolicy.IceEmissionRed,
            RepairAlloyAppearancePolicy.IceEmissionGreen,
            RepairAlloyAppearancePolicy.IceEmissionBlue,
            1f);
        foreach (var propertyName in new[] { "_ColorA", "_ColorB", "_Color" })
        {
            if (RepairAlloyAppearancePolicy.ShouldTintShaderProperty(propertyName) && clone.HasProperty(propertyName))
            {
                clone.SetColor(propertyName, iceTint);
            }
        }

        foreach (var propertyName in new[] { "_Emission", "_EmissionColor" })
        {
            if (RepairAlloyAppearancePolicy.ShouldSetEmissionShaderProperty(propertyName) && clone.HasProperty(propertyName))
            {
                clone.EnableKeyword("_EMISSION");
                clone.SetColor(propertyName, iceEmission);
            }
        }

        foreach (var propertyName in new[] { "_MetallicStrength", "_Glossiness" })
        {
            if (!RepairAlloyAppearancePolicy.ShouldInspectSurfaceShaderProperty(propertyName))
            {
                continue;
            }

            var status = clone.HasProperty(propertyName)
                ? "available, value=" + clone.GetFloat(propertyName).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                : "unavailable";
            Core.Logger.Msg("Repair Alloy shader diagnostic: material='" + sourceMaterial.name + "', property='" + propertyName + "', " + status + ".");
        }

        materialClones.Add(sourceMaterial, clone);
        return clone;
    }

    public static void Register(PhysicalMaterial material)
    {
        if (ReferenceEquals(material, null)) throw new ArgumentNullException(nameof(material));

        PhysicalMaterial.CheckItems();

        var registry = GetRegistry();

        if (registry.TryGetValue(material.Hash, out var existingMaterial))
        {
            if (!ReferenceEquals(existingMaterial, material))
            {
                throw new InvalidOperationException("A different PhysicalMaterial is already registered for hash " + material.Hash + ".");
            }
        }
        else
        {
            registry.Add(material.Hash, material);
        }

        RepairAlloy = material;
    }

    internal static void AssignStableHash(HashedGeneralValue value, uint hash, string valueName)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (string.IsNullOrWhiteSpace(valueName)) throw new ArgumentException("A runtime clone name is required.", nameof(valueName));

        var hashField = typeof(HashedGeneralValue).GetField("hash", BindingFlags.Instance | BindingFlags.NonPublic);
        if (hashField == null || hashField.FieldType != typeof(int))
        {
            throw new MissingFieldException(
                typeof(HashedGeneralValue).FullName,
                "hash (expected the game's serialized Int32 HashedGeneralValue hash field)");
        }

        hashField.SetValue(value, unchecked((int)hash));
        if (value.Hash != hash)
        {
            throw new InvalidOperationException(
                "The runtime clone '" + valueName + "' did not retain its assigned stable hash " + hash + ".");
        }
    }

    private static Dictionary<uint, PhysicalMaterial> GetRegistry()
    {
        var registryType = typeof(HashedGeneralValue<PhysicalMaterial>);
        var registryField = registryType.GetField("items", BindingFlags.Static | BindingFlags.NonPublic);
        if (registryField == null)
        {
            throw new MissingFieldException(
                registryType.FullName,
                "items (expected the game's HashedGeneralValue<PhysicalMaterial> registry)");
        }

        if (registryField.GetValue(null) is not Dictionary<uint, PhysicalMaterial> registry)
        {
            throw new InvalidOperationException(
                "The game's HashedGeneralValue<PhysicalMaterial>.items registry was not initialized as Dictionary<uint, PhysicalMaterial>.");
        }

        return registry;
    }
}
