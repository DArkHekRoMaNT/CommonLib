using CommonLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    internal static class ConfigUtil
    {
        public static object LoadConfig(ICoreAPI api, Type type, ref object config, ILogger logger)
        {
            logger ??= api.Logger;

            var configAttr = type.GetCustomAttribute<ConfigAttribute>()
                ?? throw new ArgumentException($"{type} is not a config");

            Dictionary<string, JsonConfigValue<object>> jsonConfig = new();
            string filename = configAttr.Filename;

            jsonConfig = api.LoadOrCreateConfig(filename, logger, jsonConfig) ?? new();

            object defaultConfig = Activator.CreateInstance(type);

            foreach (PropertyInfo prop in GetConfigProperties(type))
            {
                if (jsonConfig.TryGetValue(prop.Name, out JsonConfigValue<object> element))
                {
                    prop.SetValue(config, ConvertType(element.Value, prop.PropertyType));
                }
                else
                {
                    object defaultValue = prop.GetValue(defaultConfig);
                    prop.SetValue(config, defaultValue);
                }
            }

            return config;
        }

        public static void SaveConfig(ICoreAPI api, Type type, object config)
        {
            var configAttr = type.GetCustomAttribute<ConfigAttribute>()
                ?? throw new ArgumentException($"{type} is not a config");

            Dictionary<string, object> jsonConfig = new();

            object defaultConfig = Activator.CreateInstance(type);

            foreach (PropertyInfo prop in GetConfigProperties(type))
            {
                object value = prop.GetValue(config);

                Type itemType = typeof(JsonConfigValue<>).MakeGenericType(typeof(object));

                object defaultValue = prop.GetValue(defaultConfig);
                object item = Activator.CreateInstance(itemType, value, defaultValue);

                var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr is not null)
                {
                    string name = nameof(JsonConfigValue<object>.Description);
                    PropertyInfo desc = itemType.GetProperty(name);
                    desc.SetValue(item, descAttr.Text);
                }

                var checkerAttr = prop.GetCustomAttribute<ValueCheckerAttribute>();
                if (checkerAttr is not null)
                {
                    string name = nameof(JsonConfigValue<object>.Limits);
                    PropertyInfo limits = itemType.GetProperty(name);
                    limits.SetValue(item, checkerAttr.GetDescription(api));
                }

                jsonConfig.Add(prop.Name, item);
            }

            string filename = configAttr.Filename;
            api.StoreModConfig(jsonConfig, filename);
        }

        public static void ValidateConfig(ICoreAPI api, Type type, ref object config, ILogger logger)
        {
            object defaultConfig = Activator.CreateInstance(type);
            foreach (PropertyInfo prop in GetConfigProperties(type))
            {
                var checkerAttr = prop.GetCustomAttribute<ValueCheckerAttribute>();
                if (checkerAttr is not null)
                {
                    object value = prop.GetValue(config);
                    if (!checkerAttr.Check(api, (IComparable)value))
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

        public static IEnumerable<PropertyInfo> GetConfigProperties(Type type)
        {
            var configAttr = type.GetCustomAttribute<ConfigAttribute>();
            foreach (PropertyInfo prop in type.GetProperties())
            {
                if (configAttr.UseAllPropertiesByDefault)
                {
                    if (prop.GetCustomAttribute<ConfigIgnoreAttribute>() is null)
                    {
                        yield return prop;
                    }
                }
                else
                {
                    if (prop.GetCustomAttribute<ConfigPropertyAttribute>() is not null)
                    {
                        yield return prop;
                    }
                }
            }
        }

        public static byte[] SerializeServerPacket(object config)
        {
            var dict = new Dictionary<string, object>();
            foreach (PropertyInfo prop in GetConfigProperties(config.GetType()))
            {
                if (prop.GetCustomAttribute<ClientOnlyAttribute>() is null)
                {
                    dict.Add(prop.Name, prop.GetValue(config));
                }
            }
            string json = JsonConvert.SerializeObject(dict);
            return Encoding.UTF8.GetBytes(json);
        }

        public static object DeserializeServerPacket(object config, byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            foreach (PropertyInfo prop in GetConfigProperties(config.GetType()))
            {
                if (dict.TryGetValue(prop.Name, out object value))
                {
                    prop.SetValue(config, ConvertType(value, prop.PropertyType));
                }
            }
            return config;
        }

        public static object ConvertType(object value, Type type)
        {
            if (type.IsArray)
            {
                var list = (IList)value;
                Type elementType = type.GetElementType();
                Array convertedArray = Array.CreateInstance(elementType, list.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    object element = Convert.ChangeType(list[i], elementType);
                    convertedArray.SetValue(element, i);
                }
                return convertedArray;
            }
            else
            {
                return Convert.ChangeType(value, type);
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
