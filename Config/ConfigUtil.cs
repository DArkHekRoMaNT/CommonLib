using CommonLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    internal static class ConfigUtil
    {
        private static readonly object ConfigPropertyCacheLock = new();
        private static readonly Dictionary<Type, ConfigPropertyMeta[]> ConfigPropertyCache = new();
        private static readonly Type JsonConfigValueObjectType = typeof(JsonConfigValue<>).MakeGenericType(typeof(object));
        private static readonly PropertyInfo JsonConfigDescriptionProperty = JsonConfigValueObjectType.GetProperty(nameof(JsonConfigValue<object>.Description))!;
        private static readonly PropertyInfo JsonConfigLimitsProperty = JsonConfigValueObjectType.GetProperty(nameof(JsonConfigValue<object>.Limits))!;

        public static object LoadConfig(ICoreAPI api, Type type, ref object config, out int loadedVersion, ILogger logger)
        {
            var configAttr = type.GetCustomAttribute<ConfigAttribute>() ?? throw new ArgumentException($"{type} is not a config");
            var jsonConfig = api.LoadOrCreateConfig(configAttr.Filename, logger ?? api.Logger, new Dictionary<string, JsonConfigValue<object>>()) ?? [];
            var defaultConfig = Activator.CreateInstance(type);
            var configProps = GetConfigPropertyMeta(type);

            loadedVersion = -1;
            if (configAttr.Version != -1 &&
                !configProps.Any(x => x.Property.Name == "Version") &&
                jsonConfig.TryGetValue("Version", out var version))
            {
                loadedVersion = (int)ConvertType(version.Value, typeof(int), -1);
            }

            foreach (var meta in configProps)
            {
                var prop = meta.Property;
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
            var configProps = GetConfigPropertyMeta(type);

            if (configAttr.Version != -1 && !configProps.Any(x => x.Property.Name == "Version"))
            {
                var jsonItem = Activator.CreateInstance(JsonConfigValueObjectType, configAttr.Version, configAttr.Version)!;
                jsonConfig.Add("Version", jsonItem);
            }

            foreach (var meta in configProps)
            {
                var prop = meta.Property;
                var value = prop.GetValue(config);
                var defaultValue = prop.GetValue(defaultConfig);

                if (value is Enum valueEnum && defaultValue is Enum defaultValueEnum)
                {
                    value = valueEnum.ToString();
                    defaultValue = defaultValueEnum.ToString();
                }

                var jsonItem = Activator.CreateInstance(JsonConfigValueObjectType, value, defaultValue)!;
                var desc = JsonConfigDescriptionProperty;
                var limits = JsonConfigLimitsProperty;

                var descAttr = meta.Description;
                if (descAttr != null)
                {
                    desc.SetValue(jsonItem, descAttr.Text);
                }

                var checkerAttr = meta.ValueChecker;
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

            foreach (var meta in GetConfigPropertyMeta(type))
            {
                var checkerAttr = meta.ValueChecker;
                if (checkerAttr != null)
                {
                    var prop = meta.Property;
                    var valueObj = prop.GetValue(config);
                    if (valueObj == null)
                    {
                        var defaultValue = prop.GetValue(defaultConfig);
                        logger.Warning($"{type.FullName}.{prop.Name} value is null, set to default {defaultValue}");
                        prop.SetValue(config, defaultValue);
                        continue;
                    }

                    if (valueObj is not IComparable comparableValue)
                    {
                        var defaultValue = prop.GetValue(defaultConfig);
                        logger.Warning($"{type.FullName}.{prop.Name} value {valueObj} is not comparable, set to default {defaultValue}");
                        prop.SetValue(config, defaultValue);
                        continue;
                    }

                    if (!checkerAttr.Check(api, comparableValue))
                    {
                        if (checkerAttr is RangeAttribute rangeAttr)
                        {
                            var clampedValue = rangeAttr.ClampRange(comparableValue);
                            if (!Equals(clampedValue, comparableValue))
                            {
                                logger.Warning($"{type.FullName}.{prop.Name} value {comparableValue} out of bounds, set to {clampedValue}");
                            }
                            prop.SetValue(config, clampedValue);
                        }
                        else
                        {
                            var defaultValue = prop.GetValue(defaultConfig);
                            logger.Warning($"{type.FullName}.{prop.Name} value {comparableValue} unlimited, set to default {defaultValue}");
                            prop.SetValue(config, defaultValue);
                        }
                    }
                }
            }
        }

        public static IEnumerable<PropertyInfo> GetConfigProperties(Type type)
        {
            foreach (var meta in GetConfigPropertyMeta(type))
            {
                yield return meta.Property;
            }
        }

        public static byte[] SerializeServerPacket(object config)
        {
            var dict = new Dictionary<string, object>();
            foreach (var meta in GetConfigPropertyMeta(config.GetType()))
            {
                if (!meta.ClientOnly)
                {
                    var prop = meta.Property;
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
            foreach (var meta in GetConfigPropertyMeta(config.GetType()))
            {
                var prop = meta.Property;
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
                    if (elementType == null)
                    {
                        throw new InvalidOperationException($"Array element type not found for {type}.");
                    }
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

        private sealed class ConfigPropertyMeta
        {
            public PropertyInfo Property { get; }
            public DescriptionAttribute? Description { get; }
            public ValueCheckerAttribute? ValueChecker { get; }
            public bool ClientOnly { get; }

            public ConfigPropertyMeta(PropertyInfo property)
            {
                Property = property;
                Description = property.GetCustomAttribute<DescriptionAttribute>();
                ValueChecker = property.GetCustomAttribute<ValueCheckerAttribute>();
                ClientOnly = property.GetCustomAttribute<ClientOnlyAttribute>() != null;
            }
        }

        private static ConfigPropertyMeta[] GetConfigPropertyMeta(Type type)
        {
            lock (ConfigPropertyCacheLock)
            {
                if (ConfigPropertyCache.TryGetValue(type, out var cached))
                {
                    return cached;
                }
            }

            var configAttr = type.GetCustomAttribute<ConfigAttribute>() ?? throw new ArgumentException($"{type} is not a config");
            var props = type.GetProperties();
            var list = new List<ConfigPropertyMeta>(props.Length);

            foreach (var prop in props)
            {
                bool include = configAttr.UseAllPropertiesByDefault
                    ? prop.GetCustomAttribute<ConfigIgnoreAttribute>() == null
                    : prop.GetCustomAttribute<ConfigPropertyAttribute>() != null;

                if (!include)
                {
                    continue;
                }

                list.Add(new ConfigPropertyMeta(prop));
            }

            var meta = list.ToArray();
            lock (ConfigPropertyCacheLock)
            {
                ConfigPropertyCache[type] = meta;
            }

            return meta;
        }
    }
}
