using CommonLib.Config;
using CommonLib.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib.Commands
{
    public class ConfigCommand
    {
        public static void Create(ICoreAPI api, ILogger logger)
        {
            return; // TODO

            var manager = api.ModLoader.GetModSystem<ConfigManager>();
            var command = api.ChatCommands
                .GetOrCreate("cl")
                .BeginSubCommand("config")
                    .WithRootAlias("config")
                    .WithDescription("Runtime config editor for some mods using CommonLib")
                    .RequiresPrivilege(Privilege.controlserver);

            foreach (ConfigContainer config in manager.Configs)
            {
                if (!string.IsNullOrWhiteSpace(config.Name))
                {
                    var subCommand = command.BeginSubCommand(config.Name);

                    foreach (ConfigPropertyData prop in config.Properties.Values)
                    {
                        PropertyInfo propInfo = prop.Type.GetProperty(prop.Name)!;
                        subCommand
                            .BeginSubCommand(prop.Type.Name)
                                .WithArgs(GetParser("value", propInfo, api))
                                .HandleWith(args => OnShowOrSetEntry(prop.Type, propInfo, args, api, manager))
                            .EndSubCommand();
                    }

                    subCommand.EndSubCommand();
                }
            }
            command.EndSubCommand();
        }

        private static TextCommandResult OnShowOrSetEntry(Type configType, PropertyInfo prop, TextCommandCallingArgs args, ICoreAPI api, ConfigManager manager)
        {
            ConfigContainer config = manager.Configs[configType];

            if (args.Parsers[0].IsMissing)
            {
                if (prop.PropertyType.IsArray)
                {
                    var list = new List<string>();

                    foreach (object value in (IEnumerable)prop.GetValue(config)!)
                    {
                        list.Add(value.ToString() ?? "null");
                    }

                    return TextCommandResult.Success($"{prop.Name}: [ {string.Join(", ", list)} ]");
                }

                return TextCommandResult.Success($"{prop.Name}: {prop.GetValue(config)}");
            }

            else
            {
                object value = args.LastArg;
                var checkerAttr = prop.GetCustomAttribute<ConfigValueCheckerAttribute>();

                if (checkerAttr is not null)
                {
                    if (!checkerAttr.IsValid(api, value))
                    {
                        return TextCommandResult.Error($"Invalid value. {checkerAttr.GetHelpDescription(api)}");
                    }
                }

                prop.SetValue(config, value);
                manager.MarkConfigDirty(configType);

                return TextCommandResult.Success($"Set value {value} to {args.SubCmdCode}");
            }
        }

        private static ICommandArgumentParser GetParser(string name, PropertyInfo prop, ICoreAPI api)
        {
            var parsers = api.ChatCommands.Parsers;
            var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();

            switch (prop.PropertyType.Name)
            {
                case nameof(Int32):
                    if (rangeAttr is not null)
                    {
                        return parsers.OptionalIntRange(name, rangeAttr.GetMin<int>(), rangeAttr.GetMax<int>());
                    }
                    return parsers.OptionalInt(name);

                case nameof(Int64):
                    if (rangeAttr is not null)
                    {
                        return parsers.OptionalLongRange(name, rangeAttr.GetMin<long>(), rangeAttr.GetMax<long>());
                    }
                    return parsers.OptionalLong(name);

                case nameof(Single):
                    if (rangeAttr is not null)
                    {
                        return parsers.OptionalFloatRange(name, rangeAttr.GetMin<float>(), rangeAttr.GetMax<float>());
                    }
                    return parsers.OptionalFloat(name);

                case nameof(Double):
                    if (rangeAttr is not null)
                    {
                        return parsers.OptionalDoubleRange(name, rangeAttr.GetMin<double>(), rangeAttr.GetMax<double>());
                    }
                    return parsers.OptionalDouble(name);

                case nameof(Boolean):
                    return parsers.OptionalBool(name);

                case nameof(String):
                    return parsers.OptionalWord(name);
            }

            return parsers.Unparsed(name);
        }

    }
}
