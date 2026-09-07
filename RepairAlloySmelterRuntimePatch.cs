using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Alta;
using Alta.Blacksmithing;
using Alta.Chunks;
using Alta.Inventory;
using Alta.Networking;
using HarmonyLib;
using UnityEngine;

namespace RepairHammer;

/// <summary>
/// Makes the custom two-input recipe require the live vanilla Small Hammer
/// Mould, and gives only its spawned output the registered Repair Alloy.
/// </summary>
[HarmonyPatch]
internal static class RepairAlloySmelterRuntimePatch
{
    private static readonly FieldInfo CurrentMouldField = typeof(Smelter)
        .GetField("currentMould", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(Smelter).FullName, "currentMould");

    private static readonly FieldInfo CurrentRecipeField = typeof(Smelter)
        .GetField("recipe", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(Smelter).FullName, "recipe");

    private static readonly MethodInfo SpawnRecipeOutputMethod = typeof(RepairAlloySmelterRuntimePatch)
        .GetMethod(nameof(SpawnRecipeOutput), BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(typeof(RepairAlloySmelterRuntimePatch).FullName, nameof(SpawnRecipeOutput));

    private static readonly Type[] SpawnParameterTypes =
    {
        typeof(NetworkPrefab),
        typeof(SpawnData),
        typeof(Chunk),
        typeof(Vector3),
        typeof(Quaternion),
        typeof(SpawnHelper.SpawningCallback)
    };

    private static readonly MethodInfo VanillaSpawnMethod = AccessTools.Method(
        typeof(SpawnHelper),
        nameof(SpawnHelper.Spawn), SpawnParameterTypes)
        ?? throw new MissingMethodException(typeof(SpawnHelper).FullName, nameof(SpawnHelper.Spawn));

    private static MethodBase? TargetMethod()
    {
        return PatchTargetResolver.FindSmelterCompletionMoveNext();
    }

    // The recipe path calls SpawnHelper.Spawn with a null callback. Replace only
    // that call with this wrapper; the vanilla mould path already has its own
    // callback and is deliberately left untouched.
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        var code = new List<CodeInstruction>(instructions);
        var spawnIndexes = new List<int>();

        for (var index = 0; index < code.Count; index++)
        {
            if (code[index].Calls(VanillaSpawnMethod))
            {
                spawnIndexes.Add(index);
            }
        }

        var locals = original.GetMethodBody()?.LocalVariables;
        if (spawnIndexes.Count != 2
            || spawnIndexes[0] <= 0
            || code[spawnIndexes[0] - 1].opcode != System.Reflection.Emit.OpCodes.Ldnull
            || locals == null
            || locals.Count <= 1
            || locals[1].LocalType != typeof(Smelter))
        {
            Core.Logger.Warning("Repair Alloy smelter patch was not applied: the verified recipe-output spawn pattern changed.");
            return code;
        }

        code[spawnIndexes[0] - 1] = new CodeInstruction(System.Reflection.Emit.OpCodes.Ldloc_1);
        code[spawnIndexes[0]].operand = SpawnRecipeOutputMethod;
        return code;
    }

    private static NetworkEntity SpawnRecipeOutput(
        NetworkPrefab prefab,
        SpawnData spawnData,
        Chunk chunk,
        Vector3 position,
        Quaternion rotation,
        Smelter smelter)
    {
        return SpawnHelper.Spawn(
            prefab,
            spawnData,
            chunk,
            position,
            rotation,
            entity => ApplyRepairAlloyToOutput(smelter, entity));
    }

    private static void ApplyRepairAlloyToOutput(Smelter smelter, NetworkEntity entity)
    {
        if (!HasSelectedVanillaSmallHammerMould(smelter)
            || CurrentRecipeField.GetValue(smelter) is not SmeltingRecipe recipe
            || !RepairAlloyPolicy.IsExpectedRecipeHash(recipe.Hash))
        {
            return;
        }

        var pickup = entity.GetComponent<Pickup>();
        var materialPart = pickup == null ? null : pickup.PhysicalMaterial;
        var repairAlloy = RepairAlloyMaterialRegistration.RepairAlloy;
        if (pickup == null
            || pickup.Item == null
            || pickup.Item.Hash != RepairPolicy.RepairHammerHeadItemHash
            || materialPart == null
            || repairAlloy == null)
        {
            Core.Logger.Warning("Repair Alloy output was left unchanged because its pickup or registered material was unavailable.");
            return;
        }

        // SetMaterial writes the pickup's networked material hash, sends it to
        // clients, and is also the path used by save/load and reconnect sync.
        materialPart.SetMaterial(repairAlloy);
        Core.Logger.Msg("Repair Alloy output material applied: item=" + pickup.Item.Hash
            + ", material=" + repairAlloy.Hash + ".");
    }

    internal static bool HasSelectedVanillaSmallHammerMould(Smelter smelter)
    {
        if (CurrentMouldField.GetValue(smelter) is not Mould selectedMould || selectedMould.Definition == null)
        {
            Core.Logger.Msg("Repair Alloy mould check: Smelter.currentMould or its definition is unavailable.");
            return false;
        }

        var selectedDefinition = selectedMould.Definition;
        if (selectedDefinition.Product == null || selectedDefinition.Product.Hash != RepairPolicy.RepairHammerHeadItemHash)
        {
            Core.Logger.Msg("Repair Alloy mould check: selected definition hash=" + selectedDefinition.Hash
                + ", product=" + (selectedDefinition.Product == null ? "<null>" : selectedDefinition.Product.Hash.ToString())
                + ".");
            return false;
        }

        var vanillaDefinition = MouldDefinition.GetDefinition(selectedDefinition.Product);
        var isVanillaSmallHammerMould = vanillaDefinition != null
            && RepairAlloyPolicy.IsVanillaSmallHammerMouldDefinition(
                selectedDefinition.Product.Hash,
                selectedDefinition.Hash,
                vanillaDefinition.Hash);
        Core.Logger.Msg("Repair Alloy mould check: selectedDefinition=" + selectedDefinition.Hash
            + ", registeredDefinition=" + (vanillaDefinition == null ? "<null>" : vanillaDefinition.Hash.ToString())
            + ", accepted=" + isVanillaSmallHammerMould + ".");
        return isVanillaSmallHammerMould;
    }
}

[HarmonyPatch(typeof(Smelter), "FindRecipe")]
internal static class RepairAlloyRecipeGatePatch
{
    private const uint RedIronIngotHash = 30996u;

    private static bool Prefix(
        Smelter __instance,
        Dictionary<Item, int> input,
        ref float requiredFuel,
        ref SmeltingRecipe? __result)
    {
        var crystalGemBlueCount = GetCount(input, RepairAlloyRecipeRegistration.CrystalGemBlueHash);
        var goldCount = GetCount(input, RepairAlloyRecipeRegistration.GoldIngotHash);
        var redIronCount = GetCount(input, RedIronIngotHash);
        var hasRepairAlloyIngredients = crystalGemBlueCount > 0 || goldCount > 0;
        var containsOnlyRepairAlloyIngredients = ContainsOnlyRepairAlloyIngredients(input);
        if (!containsOnlyRepairAlloyIngredients)
        {
            if (redIronCount > 0)
            {
                Core.Logger.Msg("Red Iron FindRecipe pass-through: inputs=" + input.Count
                    + ", Red Iron=" + redIronCount + ", Gold=" + goldCount
                    + ", Crystal Gem Blue=" + crystalGemBlueCount + ".");
            }
            if (hasRepairAlloyIngredients)
            {
                Core.Logger.Msg("Repair Alloy recipe ignored: inputs=" + input.Count
                    + ", Crystal Gem Blue=" + crystalGemBlueCount + ", Gold=" + goldCount + ".");
            }
            return true;
        }

        var countsMatch = RepairAlloySmelterPatch.ShouldApply(
            RepairPolicy.RepairHammerHeadItemHash,
            crystalGemBlueCount,
            goldCount);
        var hasVanillaSmallHammerMould = RepairAlloySmelterRuntimePatch.HasSelectedVanillaSmallHammerMould(__instance);
        Core.Logger.Msg("Repair Alloy recipe check: Crystal Gem Blue=" + crystalGemBlueCount
            + ", Gold=" + goldCount
            + ", countsMatch=" + countsMatch
            + ", vanillaSmallHammerMould=" + hasVanillaSmallHammerMould + ".");
        if (countsMatch && hasVanillaSmallHammerMould)
        {
            // Do not bypass FindRecipe: its vanilla body both finds the
            // unlocked recipe and consumes the input stack atomically.
            return true;
        }

        // FindRecipe consumes input before returning a matching recipe. Returning
        // false here is the only safe point to reject a wrong/no mould or extra
        // ingredient count without losing the player's ingots.
        requiredFuel = 0f;
        __result = null;
        return false;
    }

    // This runs only when vanilla FindRecipe was allowed to continue.  The
    // preceding gate confirms the player's ingredients and mould; this record
    // then tells us whether the live smelter's unlocked-recipe list actually
    // resolves our registered recipe or rejects it later in the vanilla path.
    private static void Postfix(
        Dictionary<Item, int> input,
        float requiredFuel,
        SmeltingRecipe? __result)
    {
        var redIronCount = GetCount(input, RedIronIngotHash);
        if (redIronCount > 0)
        {
            Core.Logger.Msg("Red Iron FindRecipe result: recipe="
                + (__result == null ? "<null>" : __result.Hash.ToString())
                + ", requiredFuel=" + requiredFuel + ".");
            return;
        }

        if (!ContainsOnlyRepairAlloyIngredients(input)
            || GetCount(input, RepairAlloyRecipeRegistration.CrystalGemBlueHash)
                < RepairAlloyPolicy.RequiredCrystalGemBlueCount
            || GetCount(input, RepairAlloyRecipeRegistration.GoldIngotHash)
                < RepairAlloyPolicy.RequiredGoldIngotCount)
        {
            return;
        }

        Core.Logger.Msg("Repair Alloy FindRecipe result: recipe="
            + (__result == null ? "<null>" : __result.Hash.ToString())
            + ", expected=" + RepairAlloyPolicy.RecipeHash
            + ", requiredFuel=" + requiredFuel + ".");
    }

    private static bool ContainsOnlyRepairAlloyIngredients(Dictionary<Item, int> input)
    {
        if (input.Count != 2)
        {
            return false;
        }

        var hasCrystalGemBlue = false;
        var hasGold = false;
        foreach (var pair in input)
        {
            if (pair.Key.Hash == RepairAlloyRecipeRegistration.CrystalGemBlueHash)
            {
                hasCrystalGemBlue = true;
            }
            else if (pair.Key.Hash == RepairAlloyRecipeRegistration.GoldIngotHash)
            {
                hasGold = true;
            }
            else
            {
                return false;
            }
        }

        return hasCrystalGemBlue && hasGold;
    }

    private static int GetCount(Dictionary<Item, int> input, uint itemHash)
    {
        foreach (var pair in input)
        {
            if (pair.Key.Hash == itemHash)
            {
                return pair.Value;
            }
        }

        return 0;
    }
}
