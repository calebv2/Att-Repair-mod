using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Alta;
using Alta.Blacksmithing;
using Alta.Inventory;
using UnityEngine;

namespace RepairHammer;

public static class RepairAlloyRecipeRegistration
{
    // Verified from the live game item registry. 8002 is not the Crystal Gem
    // Blue Item hash in this game version.
    internal const uint CrystalGemBlueHash = 45754u;
    internal const uint GoldIngotHash = 17090u;
    private const string RecipeName = "Repair Alloy Hammer Head Small";

    private static bool registered;

    // Existing smelters cache their unlocked recipes when they are spawned.
    // Keep the live recipe available so the smelter patch can deliberately
    // select it for the one approved combination even on an older smelter.
    internal static SmeltingRecipe? RegisteredRecipe { get; private set; }

    public static void Register()
    {
        if (registered)
        {
            return;
        }

        try
        {
            var repairAlloy = RepairAlloyMaterialRegistration.CreateAndRegisterRuntimeClone();
            var recipe = CreateRuntimeRecipeClone();
            ValidateRuntimeRecipe(recipe);

            CustomRecipesAPI.Core.SetUpSmeltingRecipe(
                recipe,
                new[]
                {
                    FindAndAllowSmelterInput(CrystalGemBlueHash, "Crystal Gem Blue"),
                    FindItem(GoldIngotHash, "Gold ingot")
                },
                new[]
                {
                    FindItem(RepairAlloyPolicy.SmallHammerMouldProductHash, "Hammer Head Small")
                });

            AddRecipeToAllSmelterUpgradeSets(recipe);
            RegisteredRecipe = recipe;
            registered = true;
            LogRedIronRecipeDefinitions();
            LogForgeMultipliers();
            ForgeableMaterialProfiles.Initialize();
            Core.Logger.Msg(
                "Registered the code-only 15 Crystal Gem Blue + 5 Gold Repair Alloy Hammer Head Small recipe with material hash "
                + repairAlloy.Hash + ".");
        }
        catch (Exception exception)
        {
            Core.Logger.Error("Repair Alloy recipe was not registered: " + exception);
        }
    }

    private static Item FindItem(uint hash, string itemName)
    {
        var item = Item.All.FirstOrDefault(candidate => candidate.Hash == hash);
        if (item == null)
        {
            throw new InvalidOperationException(itemName + " item hash " + hash + " is unavailable in this game version.");
        }

        return item;
    }

    private static Item FindAndAllowSmelterInput(uint hash, string itemName)
    {
        var item = FindItem(hash, itemName);
        if (!CustomRecipesAPI.Core.itemsToAddToSmelter.Contains(item))
        {
            CustomRecipesAPI.Core.itemsToAddToSmelter.Add(item);
        }

        return item;
    }

    private static SmeltingRecipe CreateRuntimeRecipeClone()
    {
        SmeltingRecipe.CheckItems();
        var template = SmeltingRecipe.All.FirstOrDefault(IsCompatibleTemplate);
        if (template == null)
        {
            throw new InvalidOperationException(
                "Repair Alloy cannot be created because no vanilla two-input/one-output SmeltingRecipe template is available in this game version.");
        }

        var recipe = UnityEngine.Object.Instantiate(template);
        recipe.name = RecipeName;
        RepairAlloyMaterialRegistration.AssignStableHash(recipe, RepairAlloyPolicy.RecipeHash, RecipeName);

        var inputs = ReadItemCounts(recipe, "input");
        var outputs = ReadItemCounts(recipe, "output");
        SetCount(inputs, 0, RepairAlloyPolicy.RequiredCrystalGemBlueCount);
        SetCount(inputs, 1, RepairAlloyPolicy.RequiredGoldIngotCount);
        SetCount(outputs, 0, 1);
        return recipe;
    }

    private static bool IsCompatibleTemplate(SmeltingRecipe recipe)
    {
        try
        {
            return ReadItemCounts(recipe, "input").Length == 2
                && ReadItemCounts(recipe, "output").Length == 1;
        }
        catch (MissingFieldException)
        {
            return false;
        }
    }

    private static void ValidateRuntimeRecipe(SmeltingRecipe recipe)
    {
        if (!RepairAlloyPolicy.IsExpectedRecipeHash(recipe.Hash))
        {
            throw new InvalidOperationException(
                "The runtime-cloned Repair Alloy SmeltingRecipe hash must be " + RepairAlloyPolicy.RecipeHash
                + ", but the clone supplied " + recipe.Hash + ".");
        }

        var inputs = ReadItemCounts(recipe, "input");
        var outputs = ReadItemCounts(recipe, "output");
        if (!RepairAlloyPolicy.HasExpectedSerializedRecipeShape(
                inputs.Length,
                ReadCount(inputs, 0),
                ReadCount(inputs, 1),
                outputs.Length,
                ReadCount(outputs, 0)))
        {
            throw new InvalidOperationException(
                "The runtime-cloned Repair Alloy SmeltingRecipe must have exactly two serialized inputs (15 Crystal Gem Blue then 5 Gold) and one Hammer Head Small output with count 1.");
        }

        SmeltingRecipe.CheckItems();
        var recipeRegistry = GetRecipeRegistry();
        if (recipeRegistry.ContainsKey(recipe.Hash))
        {
            throw new InvalidOperationException(
                "The Repair Alloy SmeltingRecipe hash " + recipe.Hash
                + " is already registered; choose no new hash because the assigned stable hash must be unique.");
        }
    }

    private static ItemCount[] ReadItemCounts(SmeltingRecipe recipe, string fieldName)
    {
        var field = typeof(SmeltingRecipe).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || field.GetValue(recipe) is not ItemCount[] itemCounts)
        {
            throw new MissingFieldException(
                typeof(SmeltingRecipe).FullName,
                fieldName + " (expected a serialized ItemCount[] field)");
        }

        return itemCounts;
    }

    private static int ReadCount(ItemCount[] itemCounts, int index)
    {
        if (index >= itemCounts.Length)
        {
            return int.MinValue;
        }

        var itemCount = itemCounts[index];
        var field = typeof(ItemCount).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || field.GetValue(itemCount) == null)
        {
            throw new MissingFieldException(
                typeof(ItemCount).FullName,
                "count (expected the serialized item count field)");
        }

        return Convert.ToInt32(field.GetValue(itemCount));
    }

    private static void LogRedIronRecipeDefinitions()
    {
        const uint redIronIngotHash = 30996u;
        var itemField = typeof(ItemCount).GetField("item", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(ItemCount).FullName, "item");

        foreach (var recipe in SmeltingRecipe.All)
        {
            var inputs = ReadItemCounts(recipe, "input");
            var containsRedIron = inputs.Any(input => itemField.GetValue(input) is Item item && item.Hash == redIronIngotHash);
            if (!containsRedIron)
            {
                continue;
            }

            Core.Logger.Msg("Red Iron recipe definition: hash=" + recipe.Hash
                + ", inputs=" + DescribeItemCounts(inputs, itemField)
                + ", outputs=" + DescribeItemCounts(ReadItemCounts(recipe, "output"), itemField) + ".");
        }
    }

    private static string DescribeItemCounts(ItemCount[] itemCounts, FieldInfo itemField)
    {
        return string.Join(", ", itemCounts.Select((itemCount, index) =>
        {
            var item = itemField.GetValue(itemCount) as Item;
            return (item == null ? "<null>" : item.name + "(" + item.Hash + ")") + "x" + ReadCount(itemCounts, index);
        }));
    }

    private static void LogForgeMultipliers()
    {
        var forgeMultiplierField = typeof(PhysicalMaterial).GetField("forgeMultiplier", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(PhysicalMaterial).FullName, "forgeMultiplier");

        PhysicalMaterial.CheckItems();
        foreach (var material in PhysicalMaterial.All
                     .Select(candidate => new
                     {
                         Material = candidate,
                         ForgeMultiplier = Convert.ToSingle(forgeMultiplierField.GetValue(candidate))
                     })
                     .Where(entry => ForgeMultiplierPolicy.ShouldReport(entry.ForgeMultiplier))
                     .OrderByDescending(entry => entry.ForgeMultiplier)
                     .ThenBy(entry => entry.Material.name, StringComparer.Ordinal))
        {
            Core.Logger.Msg("Forge multiplier: material='" + material.Material.name
                + "', value=" + material.ForgeMultiplier.ToString("F3") + ".");
        }
    }

    private static void SetCount(ItemCount[] itemCounts, int index, int count)
    {
        if (index >= itemCounts.Length)
        {
            throw new IndexOutOfRangeException("The runtime recipe template did not retain input/output slot " + index + ".");
        }

        var field = typeof(ItemCount).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || field.FieldType != typeof(int))
        {
            throw new MissingFieldException(
                typeof(ItemCount).FullName,
                "count (expected the serialized Int32 item count field)");
        }

        field.SetValue(itemCounts[index], count);
    }

    private static Dictionary<uint, SmeltingRecipe> GetRecipeRegistry()
    {
        var registryType = typeof(HashedGeneralValue<SmeltingRecipe>);
        var field = registryType.GetField("items", BindingFlags.Static | BindingFlags.NonPublic);
        if (field == null || field.GetValue(null) is not Dictionary<uint, SmeltingRecipe> registry)
        {
            throw new MissingFieldException(
                registryType.FullName,
                "items (expected the game's HashedGeneralValue<SmeltingRecipe> registry)");
        }

        return registry;
    }

    private static void AddRecipeToAllSmelterUpgradeSets(SmeltingRecipe recipe)
    {
        var recipesField = typeof(SmelterUpgrades).GetField("recipes", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(SmelterUpgrades).FullName, "recipes");

        foreach (var upgrades in SmelterUpgrades.All)
        {
            if (recipesField.GetValue(upgrades) is not SmeltingRecipe[] recipes)
            {
                throw new InvalidOperationException("A SmelterUpgrades asset did not expose its serialized recipe array.");
            }

            if (recipes.Any(existing => existing != null && existing.Hash == recipe.Hash))
            {
                continue;
            }

            recipesField.SetValue(upgrades, recipes.Concat(new[] { recipe }).ToArray());
        }
    }
}
