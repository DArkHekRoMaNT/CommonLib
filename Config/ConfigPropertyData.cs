using Cairo;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Reflection;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    internal sealed class ConfigPropertyData
    {
        public required string Name { get; init; }

        public required Type Type { get; init; }

        public object? Value { get; private set; }

        public object? DefaultValue { get; init; }

        public bool ClientOnly { get; init; } = false;

        public bool RequiresRestart { get; init; } = false;

        public string Description { get; init; } = "";

        public ConfigValueCheckerAttribute? ValueChecker { get; init; }

        private ConfigPropertyData() { }

        public bool TrySetValue(ICoreAPI api, object? value)
        {
            if (ValueChecker == null || ValueChecker.IsValid(api, value))
            {
                Value = value;
                return true;
            }

            return false;
        }

        public bool SetValue(ICoreAPI api, object? value)
        {
            if (TrySetValue(api, value))
            {
                return true;
            }
            else
            {
                Value = DefaultValue;
                return false;
            }
        }

        internal void SetForce(object? value)
        {
            Value = ConvertType(value, Type);
        }

        public static object? ConvertType(object? value, Type type)
        {
            if (value == null)
            {
                return null;
            }

            // simply Array
            if (type.IsArray)
            {
                var list = (IList)value;

                Type elementType = type.GetElementType()!;
                Array convertedArray = Array.CreateInstance(elementType, list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    object? element = ConvertType(list[i], elementType);
                    convertedArray.SetValue(element, i);
                }

                return convertedArray;
            }

            // collection, dict
            if (value is JArray array)
            {
                return array.ToObject(type);
            }

            // primitive
            if (value is IConvertible)
            {
                return Convert.ChangeType(value, type);
            }

            if (value is JObject obj)
            {
                return obj.ToObject(type);
            }

            throw new NotImplementedException($"Unsupporter config property type {type}");
        }

        public static ConfigPropertyData Create(object instance, PropertyInfo prop, object? defaultValue)
        {
            return new ConfigPropertyData
            {
                Name = prop.Name,
                Type = prop.PropertyType,
                Value = prop.GetValue(instance),
                DefaultValue = defaultValue,
                ClientOnly = prop.GetCustomAttribute<ClientOnlyAttribute>() != null,
                RequiresRestart = prop.GetCustomAttribute<RequiresRestartAttribute>() != null,
                Description = prop.GetCustomAttribute<DescriptionAttribute>()?.Text ?? "",
                ValueChecker = prop.GetCustomAttribute<ConfigValueCheckerAttribute>()
            };
        }
    }
}
