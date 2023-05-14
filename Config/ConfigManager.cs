using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace CommonLib.Config
{
    public class ConfigManager : ModSystem
    {
        private readonly List<string> ClientStartConfigErrors = new();
        private readonly List<string> ServerStartConfigErrors = new();

        private IServerNetworkChannel? _serverChannel;
        private ICoreAPI _api = null!;

        public Dictionary<Type, object> Configs { get; } = new();

        [Obsolete("What is it for?")]
        public string[] ConfigNames => Configs.Keys
            .Select(type => type.GetCustomAttribute<ConfigAttribute>()?.Name ?? "")
            .Where(e => !string.IsNullOrEmpty(e))
            .ToArray();

        public override double ExecuteOrder() => 0.001;

        public override void StartPre(ICoreAPI api)
        {
            _api = api;
            LoadAllConfigs();

            if (_api is ICoreServerAPI sapi)
            {
                _serverChannel = sapi.Network
                    .RegisterChannel(Mod.Info.ModID + "-config-manager")
                    .RegisterMessageType<SyncConfigPacket>();
                sapi.Event.PlayerNowPlaying += byPlayer =>
                {
                    if (ServerStartConfigErrors.Count > 0 &&
                        byPlayer.Privileges.Contains(Privilege.controlserver))
                    {
                        string list = string.Join(", ", ServerStartConfigErrors);
                        string text = $"<font color=#d0342c><strong>CommonLib:</strong></font> " +
                            $"Can't load server configs: {list}. " +
                            $"May cause problems, please check server-main.txt and report it";

                        sapi.SendMessage(byPlayer, GlobalConstants.AllChatGroups, text, EnumChatType.OwnMessage);
                    }
                };
            }

            if (_api is ICoreClientAPI capi)
            {
                capi.Network
                    .RegisterChannel(Mod.Info.ModID + "-config-manager")
                    .RegisterMessageType<SyncConfigPacket>()
                    .SetMessageHandler<SyncConfigPacket>(OnSyncConfigPacketReceived);
                capi.Event.PlayerEntitySpawn += byPlayer =>
                {
                    if (ClientStartConfigErrors.Count > 0)
                    {
                        string list = string.Join(", ", ClientStartConfigErrors);
                        capi.ShowChatMessage($"<font color=#d0342c><strong>CommonLib:</strong></font> " +
                            $"Can't load client configs: {list}. " +
                            $"May cause problems, please check client-main.txt and report it");
                    }
                };
            }

            void LoadAllConfigs()
            {
                foreach (Type type in GetAllTypesWithAttribute<ConfigAttribute>())
                {
                    try
                    {
                        object config = Activator.CreateInstance(type);
                        try
                        {
                            ConfigUtil.LoadConfig(_api, type, ref config, Mod.Logger);
                        }
                        catch (InvalidCastException e)
                        {
                            Mod.Logger.Error($"Сonfig {type.FullName} looks corrupted, a new one will be created. Error:\n{e}");
                            config = Activator.CreateInstance(type);
                        }
                        ConfigUtil.SaveConfig(_api, type, config);
                        Configs.Add(type, config);
                        Mod.Logger.Notification($"Config {type.FullName} loaded successfully");
                    }
                    catch (Exception e)
                    {
                        if (api.Side == EnumAppSide.Server)
                        {
                            ServerStartConfigErrors.Add(type.FullName);
                        }
                        else
                        {
                            ClientStartConfigErrors.Add(type.FullName);
                        }

                        Mod.Logger.Error($"Take error during load config {type.FullName}, skipped. " +
                            $"May cause problems with this mod further. Error:\n{e}");
                    }
                }
            }

            Type[] GetAllTypesWithAttribute<T>() where T : Attribute
            {
                var types = new List<Type>();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var assemblyTypes = assembly.GetExportedTypes();
                        foreach (var type in assemblyTypes)
                        {
                            if (Attribute.IsDefined(type, typeof(T)))
                            {
                                types.Add(type);
                            }
                        }
                    }
                    catch (FileNotFoundException) { }
                    catch (TypeLoadException) { }
                    catch (Exception e)
                    {
                        Mod.Logger.Warning($"Assembly {assembly.FullName} skipped. {e.GetType()}: {e.Message}");
                    }
                }
                return types.ToArray();
            }

            void OnSyncConfigPacketReceived(SyncConfigPacket packet)
            {
                Mod.Logger.Debug($"Received config {packet.TypeName} from server");
                Mod.Logger.Debug($"Client configs {string.Join(", ", Configs.Keys)}");

                Type type = Type.GetType(packet.TypeName);

                if (Configs.TryGetValue(type, out object config))
                {
                    Configs[type] = ConfigUtil.DeserializeServerPacket(config, packet.Data);
                }
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerJoin += byPlayer =>
            {
                foreach (var config in Configs)
                {
                    MarkConfigDirty(config.Key);
                }
            };
        }

        public void SaveAllConfigs(ICoreAPI api)
        {
            foreach (KeyValuePair<Type, object> config in Configs)
            {
                ConfigUtil.SaveConfig(api, config.Key, config.Value);
            }
        }

        public void MarkConfigDirty(Type type)
        {
            if (Configs.TryGetValue(type, out object config))
            {
                ConfigUtil.ValidateConfig(_api, type, ref config, Mod.Logger);
                ConfigUtil.SaveConfig(_api, type, config);
                if (_serverChannel is not null)
                {
                    byte[] data = ConfigUtil.SerializeServerPacket(config);
                    _serverChannel.BroadcastPacket(new SyncConfigPacket(data, config.GetType().FullName));
                }
            }
        }

        public T GetConfig<T>()
        {
            return (T)GetConfig(typeof(T));
        }

        public object GetConfig(Type type)
        {
            if (Configs.TryGetValue(type, out object value))
            {
                return value;
            }
            throw new KeyNotFoundException($"Unknown config type: {type.FullName}");
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        private class SyncConfigPacket
        {
            public string TypeName { get; private set; }
            public byte[] Data { get; private set; }
            private SyncConfigPacket() { Data = null!; TypeName = null!; }
            public SyncConfigPacket(byte[] data, string typeName) { Data = data; TypeName = typeName; }
        }
    }
}
