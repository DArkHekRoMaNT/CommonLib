using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace CommonLib.Utils
{
    public static class WorldUtil
    {
        /// <summary>
        /// Wrapper for GetBlock and GetItem, used if you need to get a CollectibleObject
        /// regardless of whether it is an item or a block
        /// </summary>
        public static CollectibleObject GetCollectibleObject(this IWorldAccessor world, AssetLocation code)
        {
            return (CollectibleObject)world.GetItem(code) ?? world.GetBlock(code);
        }

        /// <summary>
        /// Checks if the player can break a block at the given coordinates (claim check)
        /// </summary>
        [Obsolete("Use IWorldAccessor.Claims.TryAccess and TestAccess")]
        public static bool IsPlayerCanBreakBlock(this IWorldAccessor world, BlockPos pos, IServerPlayer byPlayer)
        {
            return world.Claims.TestAccess(byPlayer, pos, EnumBlockAccessFlags.BuildOrBreak) == EnumWorldAccessResponse.Granted;
        }

        /// <summary>
        /// Converts absolute coordinates to coordinates relative to the world spawn
        /// </summary>
        public static Vec3d RelativePos(this Vec3d pos, ICoreAPI api)
        {
            pos.X -= api.World.DefaultSpawnPosition.XYZ.X;
            pos.Z -= api.World.DefaultSpawnPosition.XYZ.Z;

            return pos;
        }
    }
}
