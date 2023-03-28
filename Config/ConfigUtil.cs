using CommonLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    internal static class ConfigUtil
    {
        internal static object LoadConfig(ICoreAPI api, Type type, ref object config, ILogger logger)
        {
            logger ??= api.Logger;

            var configAttr = type.GetAttribute<ConfigAttribute>()
                ?? throw new ArgumentException($"{type} is not a config");

            Dictionary<string, JsonConfigValue<object>> jsonConfig = new();
            string filename = configAttr.Filename;

            jsonConfig = api.LoadOrCreateConfig(filename, logger, jsonConfig) ?? new();

            object defaultConfig = Activator.CreateInstance(type);

            foreach (PropertyInfo prop in GetConfigItems(type))
            {
                var attr = prop.GetAttribute<ConfigValueAttribute>();
                if (attr != null)
                {
                    if (jsonConfig.TryGetValue(prop.Name, out JsonConfigValue<object> value))
                    {
                        prop.SetValue(config, Convert.ChangeType(value.Value, prop.GetType()));
                    }
                    else
                    {
                        object defaultValue = prop.GetValue(defaultConfig);
                        prop.SetValue(config, defaultValue);
                    }
                }
            }

            return config;
        }

        internal static void SaveConfig(ICoreAPI api, Type type, object config)
        {
            var configAttr = type.GetAttribute<ConfigAttribute>()
                ?? throw new ArgumentException($"{type} is not a config");

            Dictionary<string, object> jsonConfig = new();

            object defaultConfig = Activator.CreateInstance(type);

            foreach (PropertyInfo prop in GetConfigItems(type))
            {
                var attr = prop.GetAttribute<ConfigValueAttribute>();
                if (attr != null)
                {
                    object value = prop.GetValue(config);

                    Type itemType = typeof(JsonConfigValue<>).MakeGenericType(typeof(object));

                    object defaultValue = prop.GetValue(defaultConfig);
                    object item = Activator.CreateInstance(itemType, value, defaultValue);

                    var descAttr = prop.GetAttribute<DescriptionAttribute>();
                    if (descAttr != null)
                    {
                        string name = nameof(JsonConfigValue<object>.Description);
                        PropertyInfo desc = itemType.GetProperty(name);
                        desc.SetValue(item, descAttr.Text);
                    }

                    var checkerAttr = prop.GetAttribute<ValueCheckerAttribute>();
                    if (checkerAttr != null)
                    {
                        string name = nameof(JsonConfigValue<object>.Limits);
                        PropertyInfo limits = itemType.GetProperty(name);
                        limits.SetValue(item, checkerAttr.GetDescription());
                    }

                    jsonConfig.Add(prop.Name, item);
                }
            }

            string filename = configAttr.Filename;
            api.StoreModConfig(jsonConfig, filename);
        }

        internal static object CheckConfig(Type type, object config, ILogger logger)
        {
            object defaultConfig = Activator.CreateInstance(type);
            foreach (PropertyInfo prop in GetConfigItems(type))
            {
                var attr = prop.GetAttribute<ConfigValueAttribute>();
                if (attr != null)
                {
                    var checkerAttr = prop.GetAttribute<ValueCheckerAttribute>();
                    if (checkerAttr != null)
                    {
                        object value = prop.GetValue(config);
                        if (!checkerAttr.Check((IComparable)value))
                        {
                            if (checkerAttr is RangeAttribute rangeAttr)
                            {
                                object clampedValue = rangeAttr.ClampRange((IComparable)value);
                                if (clampedValue != value)
                                {
                                    logger.Warning($"{type.FullName}.{prop.Name} value {value} out of bounds, set to {clampedValue}");
                                }
                                prop.SetValue(config, clampedValue);
                            }
                            else
                            {
                                object defaultValue = prop.GetValue(defaultConfig);
                                logger.Warning($"{type.FullName}.{prop.Name} value {value} unlimited, set to default {defaultValue}");
                                prop.SetValue(config, defaultValue);
                            }
                        }

                    }
                }
            }

            return config;
        }

        internal static IEnumerable<PropertyInfo> GetConfigItems(Type type)
        {
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (prop.GetCustomAttributes(typeof(ConfigValueAttribute), true).Length > 0)
                {
                    yield return prop;
                }
            }
        }

        internal static byte[] SerializeServerPacket(object config)
        {
            var dict = new Dictionary<string, object>();
            foreach (PropertyInfo prop in config.GetType().GetProperties())
            {
                var attr = prop.GetAttribute<ConfigValueAttribute>();
                var clientOnlyAttr = prop.GetAttribute<ClientOnlyAttribute>();
                if (attr != null && clientOnlyAttr == null)
                {
                    dict.Add(prop.Name, prop.GetValue(config));
                }
            }
            string json = JsonConvert.SerializeObject(dict);
            return Encoding.UTF8.GetBytes(json);
        }

        internal static object DeserializeServerPacket(object config, byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            foreach (PropertyInfo prop in config.GetType().GetProperties().Reverse())
            {
                var attr = prop.GetAttribute<ConfigValueAttribute>();
                if (attr != null && dict.TryGetValue(prop.Name, out object value))
                {
                    prop.SetValue(config, Convert.ChangeType(value, prop.GetType()));
                }
            }
            return config;
        }

        internal static IEnumerable<string> GetAll(Type type, object config)
        {
            foreach (PropertyInfo prop in type.GetProperties())
            {
                var attr = prop.GetAttribute<ConfigValueAttribute>();
                if (attr != null)
                {
                    yield return prop.Name + ": " + prop.GetValue(config);
                }
            }
        }

        private class JsonConfigValue<T>
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? Description { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? Limits { get; set; }

            public T Default { get; }

            public T Value { get; }

            public JsonConfigValue(T value, T defaultValue)
            {
                Value = value;
                Default = defaultValue;
            }
        }
    }
}
