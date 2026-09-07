using Alta;
using Alta.Impact;
using System.Linq;

namespace RepairHammer;

public static class RepairHammerIdentity
{
    public static bool IsRepairHammer(ImpactTool? tool)
    {
        return FindRepairHammerHead(tool) != null;
    }

    /// <summary>
    /// Finds the attached Repair Alloy small hammer head. ImpactTool.Pickup is
    /// the wooden handle after assembly, so it must never be used for the
    /// Repair Hammer's durability cost.
    /// </summary>
    public static Pickup? FindRepairHammerHead(ImpactTool? tool)
    {
        if (tool == null)
        {
            return null;
        }

        return tool.transform.root
            .GetComponentsInChildren<Pickup>(true)
            .FirstOrDefault(IsRepairHammer);
    }

    public static bool IsRepairHammer(Pickup? pickup)
    {
        if (pickup == null || pickup.Item == null || pickup.Item.Hash != RepairPolicy.RepairHammerHeadItemHash)
        {
            return false;
        }

        var repairAlloy = RepairAlloyMaterialRegistration.RepairAlloy;
        var physicalMaterial = pickup.PhysicalMaterial == null ? null : pickup.PhysicalMaterial.PhysicalMaterial;
        return repairAlloy != null
            && physicalMaterial != null
            && IsRepairHammer(pickup.Item.Hash, physicalMaterial.Hash, repairAlloy.Hash);
    }

    /// <summary>
    /// Returns whether the persisted physical-material identity belongs to the
    /// vanilla Small Hammer repair variant. This deliberately does not inspect
    /// a display name, colour, or process-local marker.
    /// </summary>
    public static bool IsRepairHammer(uint itemHash, uint physicalMaterialHash, uint repairAlloyHash)
    {
        return itemHash == RepairPolicy.RepairHammerHeadItemHash
            && physicalMaterialHash == repairAlloyHash;
    }
}
