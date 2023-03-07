using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    public class ConfigClientCommand : ClientChatCommand
    {
        public ConfigClientCommand(ICoreClientAPI api, Type type)
        {
            var manager = api.ModLoader.GetModSystem<ConfigManager>();

            var attr = type.GetCustomAttributes(typeof(ConfigAttribute), true)[0];
            string name = ((ConfigAttribute)attr).Filename.Replace(".json", "");

            Command = "cfg" + name;
            Description = "";
            Syntax = $".{Command} or .{Command} [name] [value]";

            handler = (groupId, args) =>
            {
                string? name = args.PopWord();
                string? value = args.PopAll()?.Trim();

                var config = manager.GetConfig(type);

                if (name == null || value == null)
                {
                    var sb = new StringBuilder();
                    foreach (string str in ConfigUtil.GetAll(type, config))
                    {
                        sb.AppendLine(str + "");
                    }
                    api.ShowChatMessage(sb.ToString());
                    return;
                }

                string? error = null;
                if (ConfigUtil.TrySetValue(type, config, name, value, ref error))
                {
                    api.ShowChatMessage("done");
                }
                else
                {
                    api.ShowChatMessage(error);
                }
            };
        }
    }
}
