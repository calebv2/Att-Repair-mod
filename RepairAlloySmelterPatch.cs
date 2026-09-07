namespace RepairHammer;

/// <summary>
/// Pure qualification rule for the Repair Alloy smelter flow. Runtime code must
/// additionally verify the selected MouldDefinition; an item/product hash alone
/// is never sufficient to identify the mould in a live smelter.
/// </summary>
public static class RepairAlloySmelterPatch
{
    public static bool ShouldApply(uint mouldProductHash, int redIronCount, int goldCount)
    {
        return RepairAlloyPolicy.IsRepairAlloyRecipe(mouldProductHash, redIronCount, goldCount);
    }
}
