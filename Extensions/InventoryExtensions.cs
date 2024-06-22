using System.Collections.Generic;
using Vintagestory.API.Common;

namespace CommonLib.Extensions
{
    public static class InventoryExtensions
    {
        public static void FixMappings(this InventoryBase inv, IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
        {
            foreach (ItemSlot slot in inv)
            {
                if (slot.Itemstack != null)
                {
                    if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForNewMappings))
                    {
                        slot.Itemstack = null;
                    }
                    slot.Itemstack?.Collectible.OnLoadCollectibleMappings(worldForNewMappings, slot, oldBlockIdMapping, oldItemIdMapping, resolveImports);
                }
            }
        }
    }
}
