using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace CommonLib.Utils
{
    public static class TeleportUtil
    {
        public struct RandomTeleportSettings
        {
            public int Range { get; set; }
            public Vec3i? CenterPos { get; set; }
            public Action? OnTeleported { get; set; }

            public RandomTeleportSettings()
            {
                Range = -1;
            }
        }

        public static void RandomTeleport(IServerPlayer player, RandomTeleportSettings settings, ILogger? logger = null)
        {
            logger ??= player.Entity.Api.Logger;
            try
            {
                var sapi = (ICoreServerAPI)player.Entity.Api;

                int x, z;
                if (settings.Range >= 0)
                {
                    settings.CenterPos ??= player.Entity.Pos.XYZInt;

                    x = sapi.World.Rand.Next(settings.Range * 2) - settings.Range + settings.CenterPos.X;
                    z = sapi.World.Rand.Next(settings.Range * 2) - settings.Range + settings.CenterPos.Z;
                }
                else
                {
                    x = sapi.World.Rand.Next(sapi.WorldManager.MapSizeX);
                    z = sapi.World.Rand.Next(sapi.WorldManager.MapSizeZ);
                }

                int chunkSize = sapi.WorldManager.ChunkSize;
                player.Entity.TeleportToDouble(x + 0.5f, sapi.WorldManager.MapSizeY + 2, z + 0.5f);
                sapi.WorldManager.LoadChunkColumnPriority(x / chunkSize, z / chunkSize, new ChunkLoadOptions
                {
                    OnLoaded = () =>
                    {
                        int y = (int)sapi.WorldManager.GetSurfacePosY(x, z)!;
                        player.Entity.TeleportToDouble(x + 0.5f, y + 2, z + 0.5f, settings.OnTeleported);
                    }
                });
            }
            catch (Exception e)
            {
                logger.Error("Failed to teleport player to random location.");
                logger.Error(e.Message);
            }
        }
    }
}
