using System.Collections.Generic;
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
        public static bool IsPlayerCanBreakBlock(this IWorldAccessor world, BlockPos pos, IServerPlayer byPlayer)
        {
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return true;
            }

            var sapi = (ICoreServerAPI)world.Api;

            IList<LandClaim> claims = sapi.WorldManager.SaveGame.LandClaims;
            foreach (LandClaim claim in claims)
            {
                if (claim.PositionInside(pos))
                {
                    EnumPlayerAccessResult result = claim.TestPlayerAccess(byPlayer, EnumBlockAccessFlags.BuildOrBreak);
                    return result != EnumPlayerAccessResult.Denied;
                }
            }

            return true;
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
