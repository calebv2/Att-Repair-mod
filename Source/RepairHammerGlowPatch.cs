using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alta;
using HarmonyLib;
using UnityEngine;

namespace RepairHammer;

[HarmonyPatch(typeof(PhysicalMaterialPart), nameof(PhysicalMaterialPart.SetMaterial))]
internal static class RepairHammerGlowPatch
{
    private const string GlowObjectName = "Repair Hammer Glow";
    private static readonly HashSet<int> InspectedHeatComponentParts = new();

    private static readonly FieldInfo RenderersField = typeof(PhysicalMaterialPart)
        .GetField("renderers", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(PhysicalMaterialPart).FullName, "renderers");

    private static void Postfix(PhysicalMaterialPart __instance, PhysicalMaterial physicalMaterial)
    {
        var repairAlloy = RepairAlloyMaterialRegistration.RepairAlloy;
        var isRepairAlloy = repairAlloy != null
            && physicalMaterial != null
            && physicalMaterial.Hash == repairAlloy.Hash;
        if (!Application.isBatchMode)
        {
            Core.Logger.Msg("Repair Alloy glow check: receivedMaterial="
                + (physicalMaterial == null ? "<null>" : physicalMaterial.Hash.ToString())
                + ", registeredMaterial=" + (repairAlloy == null ? "<null>" : repairAlloy.Hash.ToString())
                + ", repairAlloy=" + isRepairAlloy + ".");
        }
        if (RepairGlowPolicy.ShouldInspectHeatComponents(!Application.isBatchMode, isRepairAlloy))
        {
            LogHeatComponentInventory(__instance);
        }
        RepairHammerHeatedMaterialPatch.EnsurePersistentController(__instance, isRepairAlloy);
        RemoveExistingGlowAura(__instance);
        if (!RepairGlowPolicy.ShouldApply(!Application.isBatchMode, isRepairAlloy))
        {
            return;
        }

        ApplyWhiteEmission(__instance);
    }

    private static void ApplyWhiteEmission(PhysicalMaterialPart materialPart)
    {
        if (RenderersField.GetValue(materialPart) is not Renderer[] renderers)
        {
            Core.Logger.Warning("Repair Alloy glow was not applied: the hammer head has no renderer array.");
            return;
        }

        var emissiveRendererCount = 0;
        foreach (var renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            var material = renderer.material;
            if (!material.HasProperty("_EmissionColor"))
            {
                continue;
            }

            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(
                RepairAlloyAppearancePolicy.IceEmissionRed,
                RepairAlloyAppearancePolicy.IceEmissionGreen,
                RepairAlloyAppearancePolicy.IceEmissionBlue,
                1f));
            emissiveRendererCount++;
        }

        Core.Logger.Msg("Repair Alloy glow emission applied: renderers=" + renderers.Length
            + ", emissiveRenderers=" + emissiveRendererCount + ".");
    }

    private static void LogHeatComponentInventory(PhysicalMaterialPart materialPart)
    {
        if (!InspectedHeatComponentParts.Add(materialPart.GetInstanceID()))
        {
            return;
        }

        var self = materialPart.gameObject;
        var selfComponents = self.GetComponents<Component>();
        var parentComponents = materialPart.GetComponentsInParent<Component>(includeInactive: true)
            .Where(component => component != null && component.gameObject != self);
        var childComponents = materialPart.GetComponentsInChildren<Component>(includeInactive: true)
            .Where(component => component != null && component.gameObject != self);
        Core.Logger.Msg("Repair Alloy heat-component inventory: self=["
            + string.Join(", ", selfComponents.Where(component => component != null).Select(component => component.GetType().FullName))
            + "], parents=["
            + string.Join(", ", parentComponents.Select(component => component.GetType().FullName))
            + "], children=["
            + string.Join(", ", childComponents.Select(component => component.GetType().FullName))
            + "].");
    }

    private static void RemoveExistingGlowAura(PhysicalMaterialPart materialPart)
    {
        var existing = materialPart.transform.Find(GlowObjectName);
        if (existing != null)
        {
            UnityEngine.Object.Destroy(existing.gameObject);
        }
    }
}
