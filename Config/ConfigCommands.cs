using CommonLib.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
                .Create("clconfig")
                .WithAlias("cfg")
                .WithDescription("Runtime config editor for some mod using CommonLib")
                .RequiresPrivilege(Privilege.controlserver);

            foreach (KeyValuePair<Type, object> configByType in _manager.Configs)
            {
                var type = configByType.Key;
                var config = configByType.Value;
                var configName = type.GetCustomAttribute<ConfigAttribute>()?.Name;

                if (!string.IsNullOrWhiteSpace(configName))
                {
                    var subCommand = command.BeginSubCommand(configName);

                    foreach (PropertyInfo prop in ConfigUtil.GetConfigProperties(type))
                    {
                        subCommand
                            .BeginSubCommand(prop.Name)
                                .WithArgs(GetParser("value", prop))
                                .HandleWith(args => OnShowOrSetEntry(type, prop, args))
                            .EndSubCommand();
                    }

                    subCommand.EndSubCommand();
                }
            }
        }

        private TextCommandResult OnShowOrSetEntry(Type configType, PropertyInfo prop, TextCommandCallingArgs args)
        {
            object config = _manager.GetConfig(configType);
            if (args.RawArgs.Length > 0)
            {
                object value = args.LastArg;
                var checkerAttr = configType.GetCustomAttribute<ValueCheckerAttribute>();
                if (checkerAttr is not null)
                {
                    if (!checkerAttr.Check((IComparable)value))
                    {
                        return TextCommandResult.Error($"Invalid value. {checkerAttr.GetDescription()}");
                    }
                }
                prop.SetValue(config, value);
                _manager.MarkConfigDirty(configType);
                return TextCommandResult.Success($"Set value {value} to {args.SubCmdCode}");
            }
            else
            {
                if (prop.PropertyType.IsArray)
                {
                    var list = new List<string>();
                    foreach (var value in (IEnumerable)prop.GetValue(config))
                    {
                        list.Add(value.ToString());
                    }
                    return TextCommandResult.Success($"{prop.Name}: [ {string.Join(", ", list)} ]");
                }
                return TextCommandResult.Success($"{prop.Name}: {prop.GetValue(config)}");
            }
        }

        private ICommandArgumentParser GetParser(string name, PropertyInfo prop)
        {
            var parsers = _api.ChatCommands.Parsers;
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
