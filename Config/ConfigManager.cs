using CommonLib.Extensions;
using ProtoBuf;
using System;
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
        private IServerNetworkChannel? _serverChannel;
        private ICoreAPI _api = null!;

        public Configs Configs { get; private set; } = null!;

        public override double ExecuteOrder() => 0.001;

        public override void StartPre(ICoreAPI api)
        {
            _api = api;

            Configs = new Configs(api, Mod.Logger);
            Configs.LoadAll();

            if (_api is ICoreServerAPI sapi)
            {
                _serverChannel = sapi.Network
                    .RegisterChannel(Mod.Info.ModID + "-config-manager")
                    .RegisterMessageType<SyncConfigPacket>();

                sapi.Event.PlayerNowPlaying += byPlayer =>
                {
                    if (Configs.Errors.Count > 0 && byPlayer.Privileges.Contains(Privilege.controlserver))
                    {
                        string list = string.Join("\t\n", Configs.Errors.Select(t =>
                        {
                            var assemblyName = new AssemblyName(t.Assembly.FullName!);
                            return $"{t.FullName}, {assemblyName.Name}@{assemblyName.Version}";
                        }));

                        string text = $"<font color=#d0342c><strong>CommonLib:</strong></font> " +
                            $"<font color=#ffcc00>Can't load client configs:" +
                            $"</font>\n\n\t{list}\n\n<font color=#ffcc00>" +
                            $"May cause problems, please check server-main.txt and report it</font>";

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
                    if (Configs.Errors.Count > 0)
                    {
                        string list = string.Join("\t\n", Configs.Errors.Select(t =>
                        {
                            var assemblyName = new AssemblyName(t.Assembly.FullName!);
                            return $"{t.FullName}, {assemblyName.Name}@{assemblyName.Version}";
                        }));

                        capi.ShowChatMessage($"<font color=#d0342c><strong>CommonLib:</strong></font> " +
                            $"<font color=#ffcc00>Can't load client configs:" +
                            $"</font>\n\n\t{list}\n\n<font color=#ffcc00>" +
                            $"May cause problems, please check client-main.txt and report it</font>");
                    }
                };
            }
        }

        private void OnSyncConfigPacketReceived(SyncConfigPacket packet)
        {
            static Assembly AssemblyResolver(AssemblyName assemblyName)
            {
                assemblyName.Version = null;
                return Assembly.Load(assemblyName);
            }

            Mod.Logger.Notification($"Received config {packet.TypeName} from server");
            Type type = Type.GetType(packet.TypeName, AssemblyResolver, null, true)!;
            ConfigContainer config = Configs[type];
            config.FromBytes(packet.Data);
            config.MarkDirty();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerJoin += byPlayer =>
            {
                foreach (ConfigContainer config in Configs)
                {
                    MarkConfigDirty(config.Type);
                }
            };
        }

        public void MarkConfigDirty(Type type)
        {
            ConfigContainer config = Configs[type];

            config.MarkDirty();

            Mod? mod = _api.ModLoader.GetMod(type.Assembly);
            if (mod?.Info.Side == EnumAppSide.Universal)
            {
                _serverChannel?.BroadcastPacket(new SyncConfigPacket
                {
                    TypeName = config.Type.AssemblyQualifiedName!,
                    Data = config.ToBytes()
                });
            }
        }

        public T GetConfig<T>() where T : class
        {
            return Configs.GetConfig<T>();
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        private struct SyncConfigPacket
        {
            public required string TypeName { get; set; }
            public required byte[] Data { get; set; }
        }
    }
}
