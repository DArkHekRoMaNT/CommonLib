using CommonLib.Extensions;
using CommonLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib.Config
{
    public class ConfigCommands : ModSystem
    {
        private ICoreAPI _api = null!;
        private ConfigManager _manager = null!;

        public override void Start(ICoreAPI api)
        {
            _api = api;
            _manager = api.ModLoader.GetModSystem<ConfigManager>();
            InitConfigCommand();
        }

        private void InitConfigCommand()
        {
            var command = _api.ChatCommands
                .Create("cfg")
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(OnShowConfigs);

            foreach (KeyValuePair<Type, object> configByType in _manager.Configs)
            {
                var type = configByType.Key;
                var config = configByType.Value;
                var configName = type.GetCustomAttribute<ConfigAttribute>()?.Filename;

                if (!string.IsNullOrWhiteSpace(configName))
                {
                    command = command
                        .BeginSubCommand(configName)
                            .HandleWith(args => OnShowEntries(type));

                    foreach (PropertyInfo prop in ConfigUtil.GetConfigProperties(type))
                    {
                        command
                            .BeginSubCommand(prop.Name)
                                .WithArgs(GetConventer("value", prop))
                                .HandleWith(args => OnSetEntry(type, prop, args))
                            .EndSubCommand();
                    }

                    command.EndSubCommand();
                }
            }
        }

        private TextCommandResult OnShowConfigs(TextCommandCallingArgs args)
        {
            IEnumerable<string> names = _manager.Configs.Keys
                .Select(type => type.GetCustomAttribute<ConfigAttribute>()?.Filename!)
                .Where(e => e != null);

            if (!names.Any())
            {
                return TextCommandResult.Success("No configs");
            }

            return TextCommandResult.Success(string.Join("\n", names));
        }

        private TextCommandResult OnShowEntries(Type type)
        {
            var sb = new StringBuilder();
            object config = _manager.GetConfig(type);
            foreach (PropertyInfo prop in ConfigUtil.GetConfigProperties(type))
            {
                sb.AppendLine(prop.Name + ": " + prop.GetValue(config));
            }
            return TextCommandResult.Success(sb.ToString());
        }

        private TextCommandResult OnSetEntry(Type configType, PropertyInfo prop, TextCommandCallingArgs args)
        {
            prop.SetValue(_manager.GetConfig(configType), args.LastArg);
            _manager.MarkConfigDirty(configType);
            return TextCommandResult.Success("done");
        }

        private ICommandArgumentParser GetConventer(string name, PropertyInfo prop)
        {
            var parsers = _api.ChatCommands.Parsers;
            var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
            switch (prop.PropertyType.Name)
            {
                case "int":
                    if (rangeAttr != null)
                    {
                        return parsers.IntRange(name, (int)rangeAttr.Min, (int)rangeAttr.Max);
                    }
                    return parsers.Int(name);

                case "long":
                    if (rangeAttr != null)
                    {
                        return parsers.LongRange(name, (long)rangeAttr.Min, (long)rangeAttr.Max);
                    }
                    return parsers.Long(name);

                case "float":
                    if (rangeAttr != null)
                    {
                        return parsers.FloatRange(name, (float)rangeAttr.Min, (float)rangeAttr.Max);
                    }
                    return parsers.Float(name);

                case "double":
                    if (rangeAttr != null)
                    {
                        return parsers.DoubleRange(name, (double)rangeAttr.Min, (double)rangeAttr.Max);
                    }
                    return parsers.Double(name);

                case "bool":
                    return parsers.Bool(name);

                case "string":
                    return parsers.Word(name);
            }

            return parsers.Unparsed(name);
        }
    }
}
