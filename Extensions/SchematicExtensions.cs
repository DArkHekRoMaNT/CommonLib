using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CommonLib.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.WorldEdit;

namespace CommonLib.Extensions
{
    public static class SchematicExtensions
    {
        public static void Paste(this BlockSchematic schematic, IWorldAccessor world,
            BlockPos startPos, bool replaceMetaBlocks = true, EnumOrigin origin = EnumOrigin.BottomCenter)
        {
            BlockPos originPos = schematic.GetStartPos(startPos, origin);
            if (replaceMetaBlocks)
            {
                schematic.Place(world.BlockAccessor, world, originPos, true);
            }
            else
            {
                schematic.Place(world.BulkBlockAccessor, world, originPos, false);
                world.BulkBlockAccessor.Commit();
                schematic.PlaceEntitiesAndBlockEntitiesRaw(world.BlockAccessor, world, originPos);
            }
        }

        internal static void PlaceEntitiesAndBlockEntitiesRaw(this BlockSchematic schematic,
            IBlockAccessor blockAccessor, IWorldAccessor worldForResolve, BlockPos startPos)
        {
            var pos = new BlockPos(startPos.dimension);
            foreach (KeyValuePair<uint, string> element in schematic.BlockEntities)
            {
                uint index = element.Key;
                int x = (int)(index & 0x1FF);
                int y = (int)((index >> 20) & 0x1FF);
                int z = (int)((index >> 10) & 0x1FF);
                pos.Set(x + startPos.X, y + startPos.Y, z + startPos.Z);
                BlockEntity blockEntity = blockAccessor.GetBlockEntity(pos);
                if ((blockEntity == null) && (blockAccessor is IWorldGenBlockAccessor))
                {
                    Block block = blockAccessor.GetBlock(pos, 1);
                    if (block.EntityClass != null)
                    {
                        blockAccessor.SpawnBlockEntity(block.EntityClass, pos);
                        blockEntity = blockAccessor.GetBlockEntity(pos);
                    }
                }

                if (blockEntity != null)
                {
                    Block block = blockAccessor.GetBlock(pos, 1);
                    IClassRegistryAPI registry = worldForResolve.ClassRegistry;
                    if (block.EntityClass != registry.GetBlockEntityClass(blockEntity.GetType()))
                    {
                        worldForResolve.Logger.Warning("Could not import block" +
                            " entity data for schematic at {0}. There is already {1}, expected {2}." +
                            " Probably overlapping ruins.", pos, blockEntity.GetType(),
                            block.EntityClass);
                    }
                    else
                    {
                        ITreeAttribute treeAttribute = schematic.DecodeBlockEntityData(element.Value);
                        treeAttribute.SetInt("posx", pos.X);
                        treeAttribute.SetInt("posy", pos.Y);
                        treeAttribute.SetInt("posz", pos.Z);
                        blockEntity.FromTreeAttributes(treeAttribute, worldForResolve);
                        blockEntity.Pos = pos.Copy();
                    }
                }
            }

            Entity entity;
            foreach (string entityEncoded in schematic.Entities)
            {
                using var ms = new MemoryStream(Ascii85.Decode(entityEncoded));
                using var reader = new BinaryReader(ms);
                string entityClass = reader.ReadString();
                entity = worldForResolve.ClassRegistry.CreateEntity(entityClass);
                entity.FromBytes(reader, isSync: false);
                entity.DidImportOrExport(startPos);
                if (blockAccessor is IWorldGenBlockAccessor worldGenBlockAccessor)
                {
                    worldGenBlockAccessor.AddEntity(entity);
                }
                else
                {
                    worldForResolve.SpawnEntity(entity);
                }
            }
        }

        public static void ImportToWorldEdit(this BlockSchematic schematic, IServerPlayer player)
        {
            if (player == null)
            {
                const string message = "SchematicExtensions.ImportToWorldEdit: player is null.";
                CommonLibLogger.Error(message);
                throw new ArgumentNullException(nameof(player), message);
            }

            var api = player.Entity?.Api;
            if (api == null)
            {
                var message = $"SchematicExtensions.ImportToWorldEdit: player API not available. PlayerUID={player.PlayerUID ?? "null"}";
                CommonLibLogger.Error(message);
                throw new InvalidOperationException(message);
            }

            if (schematic == null)
            {
                var message = $"SchematicExtensions.ImportToWorldEdit: schematic is null. PlayerUID={player.PlayerUID ?? "null"}";
                api.Logger.Error(message);
                throw new ArgumentNullException(nameof(schematic), message);
            }

            var worldEdit = api.ModLoader.GetModSystem<WorldEdit>();
            if (worldEdit == null)
            {
                var message = $"SchematicExtensions.ImportToWorldEdit: WorldEdit ModSystem not found. PlayerUID={player.PlayerUID ?? "null"}";
                api.Logger.Error(message);
                throw new InvalidOperationException(message);
            }

            bool canUseWorldEdit;
            try
            {
                canUseWorldEdit = WorldEdit.CanUseWorldEdit(player, true);
            }
            catch (Exception e)
            {
                var message = $"SchematicExtensions.ImportToWorldEdit: WorldEdit.CanUseWorldEdit failed. PlayerUID={player.PlayerUID ?? "null"}";
                api.Logger.Error(message);
                api.Logger.Error(e);
                throw new InvalidOperationException(message, e);
            }

            if (canUseWorldEdit)
            {
                var clipboardBlockDataField = typeof(WorldEditWorkspace).GetField("clipboardBlockData", BindingFlags.Instance | BindingFlags.NonPublic);
                if (clipboardBlockDataField == null)
                {
                    const string message = "SchematicExtensions.ImportToWorldEdit: WorldEditWorkspace.clipboardBlockData field not found.";
                    api.Logger.Error(message);
                    throw new MissingFieldException(message);
                }

                WorldEditWorkspace workSpace;
                try
                {
                    workSpace = worldEdit.GetWorkSpace(player.PlayerUID);
                }
                catch (Exception e)
                {
                    var message = $"SchematicExtensions.ImportToWorldEdit: failed to get WorldEdit workspace. PlayerUID={player.PlayerUID ?? "null"}";
                    api.Logger.Error(message);
                    api.Logger.Error(e);
                    throw new InvalidOperationException(message, e);
                }

                if (workSpace == null)
                {
                    var message = $"SchematicExtensions.ImportToWorldEdit: WorldEdit workspace is null. PlayerUID={player.PlayerUID ?? "null"}";
                    api.Logger.Error(message);
                    throw new InvalidOperationException(message);
                }

                try
                {
                    clipboardBlockDataField.SetValue(workSpace, schematic);
                }
                catch (Exception e)
                {
                    var message = $"SchematicExtensions.ImportToWorldEdit: failed to set clipboardBlockData. PlayerUID={player.PlayerUID ?? "null"}";
                    api.Logger.Error(message);
                    api.Logger.Error(e);
                    throw new InvalidOperationException(message, e);
                }

                var caller = new Caller() { Player = player };
                try
                {
                    api.ChatCommands.ExecuteUnparsed("/we tool import", new TextCommandCallingArgs() { Caller = caller });
                    api.ChatCommands.ExecuteUnparsed("/we imc", new TextCommandCallingArgs() { Caller = caller });
                }
                catch (Exception e)
                {
                    var message = $"SchematicExtensions.ImportToWorldEdit: failed to execute WorldEdit import commands. PlayerUID={player.PlayerUID ?? "null"}";
                    api.Logger.Error(message);
                    api.Logger.Error(e);
                    throw new InvalidOperationException(message, e);
                }
            }
        }
    }
}
