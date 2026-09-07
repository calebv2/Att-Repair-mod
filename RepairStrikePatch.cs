using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Alta.Blacksmithing;
using Alta.Heat;
using Alta.Impact;
using HarmonyLib;
using UnityEngine;

namespace RepairHammer;

[HarmonyPatch]
internal static class RepairStrikePatch
{
    private static readonly FieldInfo IntegrityField = typeof(DurabilityModule).GetField("integrity", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo ReevaluateRemainingDurabilityFromIntegrityMethod = typeof(DurabilityModule).GetMethod("ReevaluateRemainingDurabilityFromIntegrity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
    private static readonly MethodInfo DurabilityChangedMethod = typeof(DurabilityModule).GetMethod("OnDurabilityChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

    private static MethodBase TargetMethod()
    {
        return PatchTargetResolver.FindForgedHitHandler()!;
    }

    private static void Postfix(ForgedModel __instance, ImpactHit impact)
    {
        if (!Application.isBatchMode || __instance == null || impact is not ImpactHitWithTool toolHit)
        {
            return;
        }

        var repairTool = toolHit.Tool;
        var hammerPickup = repairTool == null ? null : repairTool.Pickup;
        var repairHammerHead = RepairHammerIdentity.FindRepairHammerHead(repairTool);
        var isRepairHammer = repairHammerHead != null;
        var isOnAnvil = IsOnAnvil(__instance);
        var isHot = IsHot(__instance);
        Core.Logger.Msg("Repair strike check: completed=" + __instance.IsDone
            + ", hammerItem=" + (hammerPickup?.Item == null ? "<null>" : hammerPickup.Item.Hash.ToString())
            + ", hammerMaterial=" + (hammerPickup?.PhysicalMaterial?.PhysicalMaterial == null ? "<null>" : hammerPickup.PhysicalMaterial.PhysicalMaterial.Hash.ToString())
            + ", repairHammer=" + isRepairHammer
            + ", onAnvil=" + isOnAnvil
            + ", hot=" + isHot + ".");
        if (!__instance.IsDone || !isRepairHammer || !isOnAnvil || !isHot)
        {
            return;
        }

        var targetPickup = __instance.GetComponentInParent<Pickup>();
        var targetDurability = targetPickup?.DurabilityModule;
        var hammerDurability = repairHammerHead!.DurabilityModule;
        var targetMaterial = __instance.PhysicalMaterial;
        if (targetDurability == null || hammerDurability == null || targetMaterial == null)
        {
            Core.Logger.Msg("Repair durability gate: targetPickup=" + (targetPickup != null)
                + ", targetModule=" + (targetDurability != null)
                + ", repairHeadModule=" + (hammerDurability != null)
                + ", targetMaterial=" + (targetMaterial == null ? "<null>" : targetMaterial.name) + ".");
            return;
        }

        if (!ForgeableMaterialProfiles.TryGet(targetMaterial, out var profile))
        {
            Core.Logger.Msg("Repair material gate: targetMaterial='" + targetMaterial.name
                + "' is not allowed by a forge mould or has no valid forge multiplier.");
            return;
        }

        float targetIntegrity = targetDurability.Integrity;
        var canRepair = RepairPolicy.CanRepairIntegrity(true, true, targetIntegrity);
        if (!canRepair)
        {
            Core.Logger.Msg("Repair durability gate: targetIntegrity=" + targetIntegrity.ToString("F3")
                + ", canRepair=False.");
            return;
        }

        float hammerIntegrity = hammerDurability.Integrity;
        if (hammerIntegrity <= 0f)
        {
            Core.Logger.Msg("Repair durability gate: hammerIntegrity=" + hammerIntegrity.ToString("F3") + ", usable=False.");
            return;
        }

        float repairedIntegrity = RepairPolicy.RestoreIntegrity(targetIntegrity, profile.RepairFraction);
        float hammerAfter = RepairPolicy.ConsumeHammerIntegrity(hammerIntegrity, profile.HammerDamageFraction);
        WriteIntegrity(targetDurability, repairedIntegrity);
        WriteIntegrity(hammerDurability, hammerAfter);
        Core.Logger.Msg("Repair strike: targetMaterial='" + targetMaterial.name
            + "', repair=" + profile.RepairFraction.ToString("P0")
            + ", hammerDamage=" + profile.HammerDamageFraction.ToString("P0")
            + ", targetIntegrity " + targetIntegrity.ToString("F3") + " -> " + repairedIntegrity.ToString("F3")
            + "; hammerIntegrity " + hammerIntegrity.ToString("F3") + " -> " + hammerAfter.ToString("F3") + ".");
    }

    private static bool IsOnAnvil(ForgedModel forged)
    {
        if (forged.GetComponentInParent<AnvilArea>() != null)
        {
            return true;
        }

        return Physics.OverlapSphere(forged.transform.position, 0.75f)
            .Any(collider => collider.GetComponentInParent<AnvilArea>() != null);
    }

    private static bool IsHot(ForgedModel forged)
    {
        var material = forged.PhysicalMaterial;
        if (material == null)
        {
            Core.Logger.Msg("Repair heat check: forged item has no physical material.");
            return false;
        }

        var forgedPointHeatPoints = forged.ForgedPoints
            .Select(point => point.HeatPoint)
            .Where(point => point != null);
        var componentHeatPoints = forged.GetComponentsInChildren<HeatPoint>(true);
        var parentHeatPoints = forged.GetComponentsInParent<HeatPoint>(true);
        var root = forged.transform.root;
        var rootHeatPoints = root == null ? Array.Empty<HeatPoint>() : root.GetComponentsInChildren<HeatPoint>(true);
        var heatPoints = forgedPointHeatPoints
            .Concat(componentHeatPoints)
            .Where(point => point != null)
            .Distinct()
            .ToArray();
        var temperatures = new StringBuilder();
        foreach (var point in heatPoints)
        {
            if (point == null)
            {
                continue;
            }

            if (temperatures.Length > 0)
            {
                temperatures.Append(", ");
            }

            temperatures.Append(point.Temperature.ToString("F1"));
        }

        var isHot = heatPoints.Any(point => point != null && point.Temperature >= material.GlowingStart);
        Core.Logger.Msg("Repair heat check: forgedPoints=" + forged.ForgedPoints.Count
            + ", resolvedPoints=" + heatPoints.Length
            + ", componentPoints=" + componentHeatPoints.Length
            + ", parentPoints=" + parentHeatPoints.Length
            + ", root='" + (root == null ? "<null>" : root.name) + "'"
            + ", rootPoints=" + rootHeatPoints.Length
            + ", temperatures=[" + temperatures + "]"
            + ", glowingStart=" + material.GlowingStart.ToString("F1")
            + ", hot=" + isHot + ".");
        return isHot;
    }

    private static void WriteIntegrity(DurabilityModule module, float value)
    {
        IntegrityField.SetValue(module, value);
        ReevaluateRemainingDurabilityFromIntegrityMethod.Invoke(module, null);
        DurabilityChangedMethod.Invoke(module, new object[] { true });
    }
}
