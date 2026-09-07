using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alta;
using UnityEngine;

namespace RepairHammer;

public static class RepairAlloyMaterialRegistration
{
    private const string NormalHammerAppearanceMaterialName = "Iron";
    private const string RepairAlloyMaterialName = "Repair Alloy";

    public static PhysicalMaterial? RepairAlloy { get; private set; }

    /// <summary>
    /// Clones the vanilla Iron physical material at runtime. The clone retains
    /// Iron's renderer/material configuration while receiving a separate,
    /// persistent Repair Alloy identity.
    /// </summary>
    public static PhysicalMaterial CreateAndRegisterRuntimeClone()
    {
        if (RepairAlloy != null)
        {
            return RepairAlloy;
        }

        PhysicalMaterial.CheckItems();
        var template = PhysicalMaterial.All.FirstOrDefault(
            material => string.Equals(material.name, NormalHammerAppearanceMaterialName, StringComparison.Ordinal));
        if (template == null)
        {
            throw new InvalidOperationException(
                "Repair Alloy cannot be created because the vanilla '" + NormalHammerAppearanceMaterialName
                + "' PhysicalMaterial template is unavailable in this game version.");
        }

        var repairAlloy = UnityEngine.Object.Instantiate(template);
        repairAlloy.name = RepairAlloyMaterialName;
        AssignStableHash(repairAlloy, RepairAlloyPolicy.RepairAlloyMaterialHash, RepairAlloyMaterialName);
        Register(repairAlloy);
        return repairAlloy;
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
