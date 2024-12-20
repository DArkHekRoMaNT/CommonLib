using CommonLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    internal static class ConfigUtil
    {
        public static object LoadConfig(ICoreAPI api, Type type, ref object config, ILogger logger)
        {
            var configAttr = type.GetCustomAttribute<ConfigAttribute>() ?? throw new ArgumentException($"{type} is not a config");
            var jsonConfig = api.LoadOrCreateConfig(configAttr.Filename, logger ?? api.Logger, new Dictionary<string, JsonConfigValue<object>>()) ?? [];
            var defaultConfig = Activator.CreateInstance(type);

            foreach (var prop in GetConfigProperties(type))
            {
                var defaultValue = prop.GetValue(defaultConfig);
                if (jsonConfig.TryGetValue(prop.Name, out var element))
                {
                    prop.SetValue(config, ConvertType(element.Value, prop.PropertyType, defaultValue));
                }
                else
                {
                    prop.SetValue(config, defaultValue);
                }
            }

            return config;
        }

        public static void SaveConfig(ICoreAPI api, Type type, object config)
        {
            var configAttr = type.GetCustomAttribute<ConfigAttribute>() ?? throw new ArgumentException($"{type} is not a config");
            var jsonConfig = new Dictionary<string, object>();
            var defaultConfig = Activator.CreateInstance(type);

            foreach (var prop in GetConfigProperties(type))
            {
                var value = prop.GetValue(config);
                var defaultValue = prop.GetValue(defaultConfig);

                if (value is Enum valueEnum && defaultValue is Enum defaultValueEnum)
                {
                    value = valueEnum.ToString();
                    defaultValue = defaultValueEnum.ToString();
                }

                var jsonItemType = typeof(JsonConfigValue<>).MakeGenericType(typeof(object));
                var jsonItem = Activator.CreateInstance(jsonItemType, value, defaultValue)!;
                var desc = jsonItemType.GetProperty(nameof(JsonConfigValue<object>.Description))!;
                var limits = jsonItemType.GetProperty(nameof(JsonConfigValue<object>.Limits))!;

                var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                if (descAttr != null)
                {
                    desc.SetValue(jsonItem, descAttr.Text);
                }

                var checkerAttr = prop.GetCustomAttribute<ValueCheckerAttribute>();
                if (checkerAttr != null)
                {
                    limits.SetValue(jsonItem, checkerAttr.GetDescription(api));
                }
                else if (prop.PropertyType.IsEnum)
                {
                    limits.SetValue(jsonItem, $"One of: {string.Join(", ", Enum.GetNames(prop.PropertyType))}");
                }

                jsonConfig.Add(prop.Name, jsonItem);
            }

            api.StoreModConfig(jsonConfig, configAttr.Filename);
        }

        public static void ValidateConfig(ICoreAPI api, Type type, ref object config, ILogger logger)
        {
            var defaultConfig = Activator.CreateInstance(type);

            foreach (var prop in GetConfigProperties(type))
            {
                var checkerAttr = prop.GetCustomAttribute<ValueCheckerAttribute>();
                if (checkerAttr != null)
                {
                    var value = prop.GetValue(config);
                    if (!checkerAttr.Check(api, (IComparable)value))
                    {
                        if (checkerAttr is RangeAttribute rangeAttr)
                        {
                            var clampedValue = rangeAttr.ClampRange((IComparable)value);
                            if (clampedValue != value)
                            {
                                logger.Warning($"{type.FullName}.{prop.Name} value {value} out of bounds, set to {clampedValue}");
                            }
                            prop.SetValue(config, clampedValue);
                        }
                        else
                        {
                            var defaultValue = prop.GetValue(defaultConfig);
                            logger.Warning($"{type.FullName}.{prop.Name} value {value} unlimited, set to default {defaultValue}");
                            prop.SetValue(config, defaultValue);
                        }
                    }

                }
            }
        }

        public static IEnumerable<PropertyInfo> GetConfigProperties(Type type)
        {
            var configAttr = type.GetCustomAttribute<ConfigAttribute>() ?? throw new ArgumentException($"{type} is not a config");

            foreach (var prop in type.GetProperties())
            {
                if (configAttr.UseAllPropertiesByDefault)
                {
                    if (prop.GetCustomAttribute<ConfigIgnoreAttribute>() == null)
                    {
                        yield return prop;
                    }
                }
                else
                {
                    if (prop.GetCustomAttribute<ConfigPropertyAttribute>() != null)
                    {
                        yield return prop;
                    }
                }
            }
        }

        public static byte[] SerializeServerPacket(object config)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in GetConfigProperties(config.GetType()))
            {
                if (prop.GetCustomAttribute<ClientOnlyAttribute>() == null)
                {
                    dict.Add(prop.Name, prop.GetValue(config)!);
                }
            }
            var json = JsonConvert.SerializeObject(dict);
            return Encoding.UTF8.GetBytes(json);
        }

        public static object DeserializeServerPacket(object config, byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json)!;
            foreach (var prop in GetConfigProperties(config.GetType()))
            {
                if (dict.TryGetValue(prop.Name, out var value))
                {
                    prop.SetValue(config, ConvertType(value, prop.PropertyType, null));
                }
            }
            return config;
        }

        public static object ConvertType(object value, Type type, object? defaultValue)
        {
            try
            {
                if (type.IsArray)
                {
                    var list = (IList)value;
                    var elementType = type.GetElementType();
                    var convertedArray = Array.CreateInstance(elementType, list.Count);
                    for (int i = 0; i < list.Count; i++)
                    {
                        var element = Convert.ChangeType(list[i], elementType);
                        convertedArray.SetValue(element, i);
                    }
                    return convertedArray;
                }
                else if (type.IsEnum)
                {
                    if (value is string stringValue)
                    {
                        if (Enum.TryParse(type, stringValue, out var enumValue))
                        {
                            return enumValue;
                        }
                        throw new InvalidCastException("Wrong enum value");
                    }
                    else
                    {
                        return Enum.ToObject(type, value);
                    }
                }
                else
                {
                    return Convert.ChangeType(value, type);
                }
            }
            catch (Exception)
            {
                Debugger.Break();
                if (defaultValue == null)
                    throw;
                return defaultValue;
            }
        }

        private class JsonConfigValue<T>(T value, T defaultValue)
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? Description { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string? Limits { get; set; }

            public T Default { get; } = defaultValue;

            public T Value { get; } = value;
        }
    }
}
