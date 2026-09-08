using System;
using System.Collections.Generic;
using System.Reflection;
using Alta;
using Alta.Heat;
using HarmonyLib;
using UnityEngine;

namespace RepairHammer;

/// <summary>
/// Keeps Repair Alloy on the visual endpoint of its own heated-material curve,
/// without changing the item's actual temperature or heat gameplay state.
/// </summary>
[HarmonyPatch(typeof(TemperatureToMaterial), "UpdateValue")]
internal static class RepairHammerHeatedMaterialPatch
{
    private static readonly int ForcedTemperaturePropertyId = Shader.PropertyToID("_ForcedTemperature");
    private static readonly HashSet<int> LoggedTemperatureComponents = new();

    private static readonly FieldInfo PhysicalMaterialField = typeof(PhysicalMaterialPart)
        .GetField("physicalMaterial", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(PhysicalMaterialPart).FullName, "physicalMaterial");

    private static readonly FieldInfo TargetRenderersField = typeof(TemperatureToMaterial)
        .GetField("targetRenderers", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(TemperatureToMaterial).FullName, "targetRenderers");

    private static readonly FieldInfo TemperatureToValueField = typeof(TemperatureToMaterial)
        .GetField("temperatureToValue", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(TemperatureToMaterial).FullName, "temperatureToValue");

    private static void Postfix(TemperatureToMaterial __instance)
    {
        ApplyForcedTemperature(__instance);
    }

    internal static void EnsurePersistentController(PhysicalMaterialPart materialPart, bool isRepairAlloy)
    {
        if (!RepairGlowPolicy.ShouldMaintainHeatedMaterialTemperature(!Application.isBatchMode, isRepairAlloy)
            || materialPart.GetComponent<RepairAlloyHeatedMaterialController>() != null)
        {
            return;
        }

        materialPart.gameObject.AddComponent<RepairAlloyHeatedMaterialController>();
    }

    internal static void ApplyForcedTemperature(TemperatureToMaterial temperatureToMaterial)
    {
        if (Application.isBatchMode)
        {
            return;
        }

        var materialPart = temperatureToMaterial.GetComponent<PhysicalMaterialPart>();
        var physicalMaterial = materialPart == null ? null : PhysicalMaterialField.GetValue(materialPart) as PhysicalMaterial;
        var repairAlloy = RepairAlloyMaterialRegistration.RepairAlloy;
        var isRepairAlloy = repairAlloy != null
            && physicalMaterial != null
            && physicalMaterial.Hash == repairAlloy.Hash;
        if (!RepairGlowPolicy.ShouldForceHeatedMaterialTemperature(isClient: true, isRepairAlloy))
        {
            return;
        }

        var curve = TemperatureToValueField.GetValue(temperatureToMaterial) as AnimationCurve;
        var renderers = TargetRenderersField.GetValue(temperatureToMaterial) as Renderer[];
        if (curve == null || renderers == null || curve.length == 0)
        {
            Core.Logger.Warning("Repair Alloy heated-material visual was not applied: its temperature curve or renderer list is unavailable.");
            return;
        }

        var keys = curve.keys;
        var curveValues = new float[keys.Length];
        for (var index = 0; index < keys.Length; index++)
        {
            curveValues[index] = keys[index].value;
        }

        var forcedValue = RepairGlowPolicy.SelectFullyHeatedTemperatureValue(curveValues);
        var props = new MaterialPropertyBlock();
        var appliedRenderers = 0;
        foreach (var renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(props);
            props.SetFloat(ForcedTemperaturePropertyId, forcedValue);
            renderer.SetPropertyBlock(props);
            appliedRenderers++;
        }

        if (LoggedTemperatureComponents.Add(temperatureToMaterial.GetInstanceID()))
        {
            Core.Logger.Msg("Repair Alloy heated-material visual applied: forcedTemperature="
                + forcedValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
                + ", renderers=" + appliedRenderers + ".");
        }
    }
}

internal sealed class RepairAlloyHeatedMaterialController : MonoBehaviour
{
    private TemperatureToMaterial? temperatureToMaterial;

    private void Awake()
    {
        temperatureToMaterial = GetComponent<TemperatureToMaterial>();
    }

    private void LateUpdate()
    {
        if (temperatureToMaterial != null)
        {
            RepairHammerHeatedMaterialPatch.ApplyForcedTemperature(temperatureToMaterial);
        }
    }
}
