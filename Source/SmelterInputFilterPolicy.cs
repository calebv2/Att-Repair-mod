using System.Collections.Generic;

namespace RepairHammer;

public static class SmelterInputFilterPolicy
{
    public static bool ShouldAddItem(IEnumerable<uint> existingItemHashes, uint candidateItemHash)
    {
        foreach (var existingItemHash in existingItemHashes)
        {
            if (existingItemHash == candidateItemHash)
            {
                return false;
            }
        }

        return true;
    }
}
