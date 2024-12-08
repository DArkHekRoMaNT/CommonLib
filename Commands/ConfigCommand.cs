using CommonLib.Config;
using CommonLib.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib.Commands
{
    public class ConfigCommand
    {
        public static void Create(ICoreAPI api, ILogger logger)
        {
            var manager = api.ModLoader.GetModSystem<ConfigManager>();
            var configCommand = api.ChatCommands
                .GetOrCreate("cl")
                .BeginSubCommand("config")
                    .WithRootAlias("config")
                    .WithDescription("Runtime config editor for some mods using CommonLib")
                    .RequiresPrivilege(api is ICoreServerAPI ? Privilege.controlserver : Privilege.chat);

            foreach (var configByType in manager.Configs)
            {
                var type = configByType.Key;
                var config = configByType.Value;
                var configName = type.GetCustomAttribute<ConfigAttribute>()?.Name;

                if (!string.IsNullOrWhiteSpace(configName))
                {
                    var props = ConfigUtil.GetConfigProperties(type);

                    var hasCommands = false;
                    foreach (PropertyInfo prop in props)
                    {
                        if (ShowOnThisSide(prop, api))
                        {
                            hasCommands = true;
                            break;
                        }
                    }

                    if (!hasCommands)
                        continue;

                    var subCommand = configCommand.BeginSubCommand(configName);

                    foreach (PropertyInfo prop in props)
                    {
                        if (!ShowOnThisSide(prop, api))
                            continue;

                        subCommand
                            .BeginSubCommand(prop.Name)
                                .WithArgs(GetParser("value", prop, api))
                                .HandleWith(args => OnShowOrSetEntry(type, prop, args))
                            .EndSubCommand();
                    }

                    subCommand.EndSubCommand();
                }
            }

            if (!configCommand.Subcommands.Any())
            {
                configCommand.HandleWith(_ => TextCommandResult.Success("No configs"));
            }

            configCommand.EndSubCommand();

            TextCommandResult OnShowOrSetEntry(Type configType, PropertyInfo prop, TextCommandCallingArgs args)
            {
                var config = manager.GetConfig(configType);
                if (args.Parsers[0].IsMissing)
                {
                    if (prop.PropertyType.IsArray)
                    {
                        var list = new List<string>();
                        foreach (var value in (IEnumerable)prop.GetValue(config)!)
                        {
                            list.Add(value?.ToString() ?? "null");
                        }
                        return TextCommandResult.Success($"{prop.Name}: [ {string.Join(", ", list)} ]");
                    }
                    return TextCommandResult.Success($"{prop.Name}: {prop.GetValue(config)}");
                }
                else
                {
                    var value = args.LastArg;
                    var checkerAttr = prop.GetCustomAttribute<ValueCheckerAttribute>();
                    if (checkerAttr != null)
                    {
                        if (!checkerAttr.Check(api, (IComparable)value))
                        {
                            return TextCommandResult.Error($"Invalid value. {checkerAttr.GetDescription(api)}");
                        }
                    }

                    if (prop.PropertyType.IsEnum)
                    {
                        value = Enum.Parse(prop.PropertyType, (string)value);
                    }

                    prop.SetValue(config, value);
                    manager.MarkConfigDirty(configType);
                    return TextCommandResult.Success($"Set value {value} to {args.SubCmdCode}");
                }
            }
        }

        private static bool ShowOnThisSide(PropertyInfo prop, ICoreAPI api)
        {
            var clientOnly = prop.GetCustomAttribute<ClientOnlyAttribute>() != null;

            if (clientOnly && api.Side == EnumAppSide.Server)
            {
                return false;
            }

            if (!clientOnly && api.Side == EnumAppSide.Client)
            {
                return false;
            }

            return true;
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
                    var stringsAttr = prop.GetCustomAttribute<StringsAttribute>();
                    if (stringsAttr != null)
                    {
                        return parsers.OptionalWordRange(name, stringsAttr.Values);
                    }
                    else
                    {
                        return parsers.OptionalWord(name);
                    }
            }

            if (prop.PropertyType.IsEnum)
            {
                return parsers.OptionalWordRange(name, Enum.GetNames(prop.PropertyType));
            }

            return parsers.Unparsed(name);
        }
    }
}
