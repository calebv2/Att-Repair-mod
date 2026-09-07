using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alta.Inventory;
using Alta.Networking;
using HarmonyLib;

namespace RepairHammer;

/// <summary>
/// Applies the Crystal Gem Blue input permission to every smelter as its
/// NetworkPrefab initializes. This is the small portion of CustomRecipesAPI
/// that Repair Hammer needs for its own recipe.
/// </summary>
[HarmonyPatch(typeof(NetworkPrefab), "Initialize")]
internal static class RepairAlloySmelterInputFilter
{
    private const uint SmelterPrefabHash = 44646u;
    private const uint SmelterOreDockEntityHash = 42990u;
    private static readonly List<Item> AllowedItems = new List<Item>();

    internal static void Allow(Item item)
    {
        if (item != null && SmelterInputFilterPolicy.ShouldAddItem(AllowedItems.Select(existing => existing.Hash), item.Hash))
        {
            AllowedItems.Add(item);
        }
    }

    private static void Postfix(NetworkPrefab __instance)
    {
        if (__instance == null || __instance.Hash != SmelterPrefabHash || AllowedItems.Count == 0)
        {
            return;
        }

        var embeddedEntitiesField = typeof(NetworkEntityParent).GetField("embeddedEntities", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(NetworkEntityParent).FullName, "embeddedEntities");
        if (embeddedEntitiesField.GetValue(__instance) is not List<NetworkEntity> embeddedEntities)
        {
            return;
        }

        var oreDockEntity = embeddedEntities.FirstOrDefault(entity => entity.Hash == SmelterOreDockEntityHash);
        var oreDock = oreDockEntity == null ? null : oreDockEntity.gameObject.GetComponent<PickupDock>();
        if (oreDock == null)
        {
            Core.Logger.Warning("Repair Alloy smelter input filter was not applied because the ore dock was unavailable.");
            return;
        }

        foreach (var item in AllowedItems)
        {
            if (SmelterInputFilterPolicy.ShouldAddItem(oreDock.Settings.IncludedItems.Select(existing => existing.Hash), item.Hash))
            {
                oreDock.Settings.IncludedItems.Add(item);
            }
        }
    }
}
