using CommonLib.Extensions;
using MonoMod.Core.Platforms;
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
        private readonly List<string> _clientStartConfigErrors = [];
        private readonly List<string> _serverStartConfigErrors = [];

        private IServerNetworkChannel? _serverChannel;
        private ICoreAPI _api = null!;

        public Dictionary<Type, object> Configs { get; } = [];

        public override double ExecuteOrder() => 0.001;

        public override void StartPre(ICoreAPI api)
        {
            _api = api;
            LoadAllConfigs();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _serverChannel = api.Network
                .RegisterChannel(Constants.ConfigManagerChannelName)
                .RegisterMessageType<SyncConfigPacket>();

            api.Event.PlayerNowPlaying += byPlayer =>
            {
                if (_serverStartConfigErrors.Count > 0 &&
                    byPlayer.Privileges.Contains(Privilege.controlserver))
                {
                    var text = $"<font color=#d0342c><strong>CommonLib:</strong></font> " +
                        $"Can't load server configs:\n\n{string.Join("\n", _serverStartConfigErrors)}\n\n" +
                        $"May cause problems, please check <font color=#ffa500>server-main.log</font> and report it";
                    api.SendMessage(byPlayer, GlobalConstants.AllChatGroups, text, EnumChatType.OwnMessage);
                }
            };

            api.Event.PlayerJoin += byPlayer =>
            {
                foreach (var config in Configs)
                {
                    MarkConfigDirty(config.Key);
                }
            };
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Network
                .RegisterChannel(Constants.ConfigManagerChannelName)
                .RegisterMessageType<SyncConfigPacket>()
                .SetMessageHandler<SyncConfigPacket>(OnSyncConfigPacketReceived);

            api.Event.PlayerEntitySpawn += byPlayer =>
            {
                if (_clientStartConfigErrors.Count > 0)
                {
                    api.ShowChatMessage($"<font color=#d0342c><strong>CommonLib:</strong></font> " +
                        $"Can't load client configs:\n\n{string.Join("\n", _clientStartConfigErrors)}\n\n" +
                        $"May cause problems, please check <font color=#ffa500>client-main.log</font> and report it");
                }
            };
        }

        private void LoadAllConfigs()
        {
            foreach (Type type in GetAllTypesWithAttribute<ConfigAttribute>())
            {
                if (!_api.ModLoader.IsModEnabled(type.Assembly))
                {
                    Mod.Logger.Notification($"Сonfig {FormatAssembly(type)} skipped (mod is not enabled)");
                    continue;
                }

                try
                {
                    var config = Activator.CreateInstance(type)!;
                    try
                    {
                        ConfigUtil.LoadConfig(_api, type, ref config, Mod.Logger);
                    }
                    catch (InvalidCastException e)
                    {
                        Mod.Logger.Error($"Сonfig {FormatAssembly(type)} looks corrupted, a new one will be created. Error:\n{e}");
                        config = Activator.CreateInstance(type)!;
                    }
                    ConfigUtil.SaveConfig(_api, type, config);
                    Configs.Add(type, config);
                    Mod.Logger.Notification($"Config {FormatAssembly(type)} loaded successfully");
                }
                catch (Exception e)
                {
                    if (_api.Side == EnumAppSide.Server)
                    {
                        _serverStartConfigErrors.Add(FormatAssembly(type));
                    }
                    else
                    {
                        _clientStartConfigErrors.Add(FormatAssembly(type));
                    }

                    Mod.Logger.Error(
                        $"Take error during load config {type.AssemblyQualifiedName}, skipped. " +
                        $"May cause problems with this mod further. Error:\n{e}");
                }
            }
        }

        private Type[] GetAllTypesWithAttribute<T>() where T : Attribute
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

        private void OnSyncConfigPacketReceived(SyncConfigPacket packet)
        {
            try
            {
                var type = Type.GetType(packet.TypeName, true)!;
                if (Configs.TryGetValue(type, out var config))
                {
                    Configs[type] = ConfigUtil.DeserializeServerPacket(config, packet.Data);
                    Mod.Logger.Notification($"Received config {FormatAssembly(type)} from server");
                }
                else
                {
                    Mod.Logger.Notification($"Received unknown config {packet.TypeName} from server");
                }
            }
            catch (Exception)
            {
                Mod.Logger.Notification($"Error when receiving config {packet.TypeName} from server");
                throw;
            }
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
            if (Configs.TryGetValue(type, out var config))
            {
                ConfigUtil.ValidateConfig(_api, type, ref config, Mod.Logger);
                ConfigUtil.SaveConfig(_api, type, config);
                Mod? mod = _api.ModLoader.GetMod(type.Assembly);
                if (mod?.Info.Side == EnumAppSide.Universal)
                {
                    _serverChannel?.BroadcastPacket(new SyncConfigPacket()
                    {
                        TypeName = type.AssemblyQualifiedName ?? string.Empty,
                        Data = ConfigUtil.SerializeServerPacket(config)
                    });
                }
            }
        }

        public T GetConfig<T>()
        {
            return (T)GetConfig(typeof(T));
        }

        public object GetConfig(Type type)
        {
            if (Configs.TryGetValue(type, out var value))
            {
                return value;
            }
            throw new KeyNotFoundException($"Unknown config type: {FormatAssembly(type)}");
        }

        private static string FormatAssembly(Type type)
        {
            var assembly = type.Assembly.GetName();
            if (string.IsNullOrEmpty(assembly.Name))
            {
                return type.AssemblyQualifiedName ?? "null";
            }
            return $"{type.FullName} from {assembly.Name} v{assembly.Version}";
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        private class SyncConfigPacket
        {
            public required string TypeName { get; init; }
            public required byte[] Data { get; init; }
        }
    }
}
