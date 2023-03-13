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
            _api.ChatCommands
                .Create("cfg")
                .RequiresPrivilege(Privilege.controlserver)
                .HandleWith(OnShowConfigs);

            foreach (KeyValuePair<Type, object> configByType in _manager.Configs)
            {
                var type = configByType.Key;
                var config = configByType.Value;
                var configName = type.GetAttribute<ConfigAttribute>()?.Filename;

                if (!string.IsNullOrWhiteSpace(configName))
                {
                    var command = _api.ChatCommands
                        .GetOrCreate("cfg")
                        .BeginSubCommand(configName)
                            .HandleWith(args => OnShowEntries(type));

                    foreach (PropertyInfo prop in ConfigUtil.GetConfigItems(type))
                    {
                        var attr = prop.GetAttribute<ConfigItemAttribute>();
                        if (attr != null)
                        {
                            command
                                .BeginSubCommand(prop.Name)
                                    .WithArgs(GetConventer("value", prop.GetType(), attr))
                                    .HandleWith(args => OnSetEntry(type, prop, args))
                                .EndSubCommand();
                        }
                    }

                    command.EndSubCommand();
                }
            }
        }

        private TextCommandResult OnShowConfigs(TextCommandCallingArgs args)
        {
            IEnumerable<string> names = _manager.Configs.Keys
                .Select(type => type.GetAttribute<ConfigAttribute>()?.Filename!)
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
            foreach (string str in ConfigUtil.GetAll(type, _manager.GetConfig(type)))
            {
                sb.AppendLine(str + "");
            }
            return TextCommandResult.Success(sb.ToString());
        }

        private TextCommandResult OnSetEntry(Type configType, PropertyInfo prop, TextCommandCallingArgs args)
        {
            prop.SetValue(_manager.GetConfig(configType), args.LastArg);
            _manager.MarkConfigDirty(configType);
            return TextCommandResult.Success("done");
        }

        private ICommandArgumentParser GetConventer(string name, Type type, ConfigItemAttribute attr)
        {
            var parsers = _api.ChatCommands.Parsers;
            switch (type.Name)
            {
                case "int":
                    int? minInt = (int?)attr.MinValue;
                    int? maxInt = (int?)attr.MaxValue;

                    if (minInt != null || maxInt != null)
                    {
                        return parsers.IntRange(name, minInt ?? int.MinValue, maxInt ?? int.MaxValue);
                    }
                    return parsers.Int(name);

                case "long":
                    long? minLong = (long?)attr.MinValue;
                    long? maxLong = (long?)attr.MaxValue;

                    if (minLong != null || maxLong != null)
                    {
                        return parsers.LongRange(name, minLong ?? long.MinValue, maxLong ?? long.MaxValue);
                    }
                    return parsers.Long(name);

                case "float":
                    float? minFloat = (float?)attr.MinValue;
                    float? maxFloat = (float?)attr.MaxValue;

                    if (minFloat != null || maxFloat != null)
                    {
                        return parsers.FloatRange(name, minFloat ?? float.MinValue, maxFloat ?? float.MaxValue);
                    }
                    return parsers.Float(name);

                case "double":
                    double? minDouble = (double?)attr.MinValue;
                    double? maxDouble = (double?)attr.MaxValue;

                    if (minDouble != null || maxDouble != null)
                    {
                        return parsers.DoubleRange(name, minDouble ?? double.MinValue, maxDouble ?? double.MaxValue);
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
