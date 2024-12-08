using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
            var pos = new BlockPos();
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
            var api = player.Entity.Api;
            var worldEdit = api.ModLoader.GetModSystem<WorldEdit>();
            if (WorldEdit.CanUseWorldEdit(player, true))
            {
                var clipboardBlockDataField = typeof(WorldEditWorkspace).GetField("clipboardBlockData", BindingFlags.Instance | BindingFlags.NonPublic);
                var workSpace = worldEdit.GetWorkSpace(player.PlayerUID);
                clipboardBlockDataField?.SetValue(workSpace, schematic);

                var caller = new Caller() { Player = player };
                api.ChatCommands.ExecuteUnparsed("/we tool import", new TextCommandCallingArgs() { Caller = caller });
                api.ChatCommands.ExecuteUnparsed("/we imc", new TextCommandCallingArgs() { Caller = caller });
            }
        }
    }
}
