namespace RepairHammer;

public static class RepairAlloyPolicy
{
    public const uint RecipeHash = 0x52414C53u;
    public const uint RepairAlloyMaterialHash = 62913u;
    // Verified from the live smelter: the vanilla Small Hammer Mould's
    // MouldDefinition.Product is Hammer Head Small, not Hammer.
    public const uint SmallHammerMouldProductHash = RepairPolicy.RepairHammerHeadItemHash;
    public const int RequiredCrystalGemBlueCount = 15;
    public const int RequiredGoldIngotCount = 5;

    public static bool IsRepairAlloyRecipe(uint mouldProductHash, int crystalGemBlueCount, int goldCount)
    {
        return mouldProductHash == SmallHammerMouldProductHash
            && crystalGemBlueCount >= RequiredCrystalGemBlueCount
            && goldCount >= RequiredGoldIngotCount;
    }

    public static bool IsExpectedRecipeHash(uint recipeHash)
    {
        return recipeHash == RecipeHash;
    }

    public static bool IsExpectedRepairAlloyMaterialHash(uint materialHash)
    {
        return materialHash == RepairAlloyMaterialHash;
    }

    /// <summary>
    /// A mould placed in a smelter can be a runtime clone of its registered
    /// definition. Unity reference identity is therefore not a valid way to
    /// recognise the vanilla Small Hammer mould; the definition hash is.
    /// </summary>
    public static bool IsVanillaSmallHammerMouldDefinition(
        uint mouldProductHash,
        uint selectedDefinitionHash,
        uint vanillaDefinitionHash)
    {
        return mouldProductHash == SmallHammerMouldProductHash
            && selectedDefinitionHash == vanillaDefinitionHash;
    }

    public static bool HasExpectedSerializedRecipeShape(
        int inputSlotCount,
        int crystalGemBlueInputCount,
        int goldInputCount,
        int outputSlotCount,
        int smallHammerOutputCount)
    {
        return inputSlotCount == 2
            && crystalGemBlueInputCount == RequiredCrystalGemBlueCount
            && goldInputCount == RequiredGoldIngotCount
            && outputSlotCount == 1
            && smallHammerOutputCount == 1;
    }
}
