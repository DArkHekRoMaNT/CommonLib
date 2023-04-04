using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib.Config
{
    public class ConfigManager : ModSystem
    {
        private IServerNetworkChannel? _serverChannel;
        private ICoreAPI _api = null!;

        public Dictionary<Type, object> Configs { get; } = new();
        public string[] ConfigNames => Configs.Keys
            .Select(type => type.GetCustomAttribute<ConfigAttribute>()?.Filename!)
            .Where(e => e is not null)
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
            }

            if (_api is ICoreClientAPI capi)
            {
                capi.Network
                    .RegisterChannel(Mod.Info.ModID + "-config-manager")
                    .RegisterMessageType<SyncConfigPacket>()
                    .SetMessageHandler<SyncConfigPacket>(OnSyncConfigPacketReceived);
            }

            void LoadAllConfigs()
            {
                foreach (Type type in GetAllTypesWithAttribute<ConfigAttribute>())
                {
                    object config = Activator.CreateInstance(type);
                    ConfigUtil.LoadConfig(_api, type, ref config, Mod.Logger);
                    ConfigUtil.SaveConfig(_api, type, config);
                    Configs.Add(type, config);
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
                }
                return types.ToArray();
            }

            void OnSyncConfigPacketReceived(SyncConfigPacket packet)
            {
                if (Configs.TryGetValue(packet.Type, out object config))
                {
                    Configs[packet.Type] = ConfigUtil.DeserializeServerPacket(config, packet.Data);
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
                ConfigUtil.ValidateConfig(type, ref config, Mod.Logger);
                ConfigUtil.SaveConfig(_api, type, config);
                if (_serverChannel is not null)
                {
                    byte[] data = ConfigUtil.SerializeServerPacket(config);
                    _serverChannel.BroadcastPacket(new SyncConfigPacket(data, config.GetType()));
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
            throw new KeyNotFoundException("Unknown config type: " + type.FullName);
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        private class SyncConfigPacket
        {
            public Type Type { get; private set; }
            public byte[] Data { get; private set; }
            private SyncConfigPacket() { Data = null!; Type = null!; }
            public SyncConfigPacket(byte[] data, Type type) { Data = data; Type = type; }
        }
    }
}
