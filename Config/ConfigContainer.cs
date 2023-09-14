using CommonLib.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace CommonLib.Config
{
    public sealed class ConfigContainer
    {
        private readonly ICoreAPI _api;
        private readonly ILogger _logger;

        public required Type Type { get; init; }
        public required object Instance { get; init; }
        public required ConfigAttribute Attribute { get; init; }

        internal Dictionary<string, ConfigPropertyData> Properties { get; } = new();

        public string Name => Attribute.Name;

        private ConfigContainer(ICoreAPI api, ILogger logger)
        {
            _api = api;
            _logger = logger;
        }

        public void LoadOrCreate()
        {
            var loadedConfig = new Dictionary<string, object?>();

            try
            {
                _api.LoadOrCreateConfig(Attribute.Filename, _logger, loadedConfig);
            }
            catch (JsonReaderException)
            {
                _logger.Warning($"Corrupted or old config for {Type.AssemblyQualifiedName}, init new");
            }

            foreach ((string name, object? value) in loadedConfig)
            {
                if (Properties.ContainsKey(name))
                {
                    ConfigPropertyData propData = Properties[name];
                    object? valueTyped = ConfigPropertyData.ConvertType(value, propData.Type);
                    propData.SetValue(_api, valueTyped);
                }
            }

            Save();
        }

        public void Save()
        {
            try
            {
                string path = Path.Combine(GamePaths.ModConfig, Attribute.Filename);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                using (var stream = new StreamWriter(path))
                {
                    using var writer = new JsonTextWriter(stream);

                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartObject();

                    int i = Properties.Count;
                    string tab = new(writer.IndentChar, writer.Indentation);
                    string commentPrefix = $"\n{tab}// ";

                    foreach (ConfigPropertyData prop in Properties.Values)
                    {
                        if (!string.IsNullOrWhiteSpace(prop.Description))
                        {
                            writer.WriteRaw($"{commentPrefix}{prop.Description}");
                        }

                        string? valueDesc = prop.ValueChecker?.GetHelpDescription(_api);
                        if (valueDesc != null)
                        {
                            writer.WriteRaw($"{commentPrefix}{valueDesc}");
                        }

                        string serializedDefault = JsonConvert.SerializeObject(prop.DefaultValue);
                        writer.WriteRaw($"{commentPrefix}Default: {serializedDefault}");

                        writer.WritePropertyName(prop.Name);

                        if (prop.Value == null)
                        {
                            writer.WriteNull();
                        }
                        else
                        {
                            JToken token = JToken.FromObject(prop.Value);
                            token.WriteTo(writer);
                        }

                        if (--i > 0)
                        {
                            writer.WriteRaw(",\n");
                        }
                    }

                    writer.WriteEnd();
                }
            }

            catch (Exception e)
            {
                _logger.Error($"Cant save config for {Type.AssemblyQualifiedName}. Error:\n{e}");
            }
        }

        public void MarkDirty()
        {
            foreach (string propName in Properties.Keys)
            {
                PropertyInfo prop = Type.GetProperty(propName)!;
                Properties[propName].SetValue(_api, prop.GetValue(Instance));
                prop.SetValue(Instance, Properties[propName].Value);
            }

            Save();
        }

        public byte[] ToBytes()
        {
            Dictionary<string, object?> dict = Properties.ToDictionary(p => p.Key, p => p.Value.Value);
            string json = JsonConvert.SerializeObject(dict);
            return Encoding.UTF8.GetBytes(json);
        }

        public void FromBytes(byte[] data)
        {
            string json = Encoding.UTF8.GetString(data);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json) ?? new();
            foreach ((string name, object? value) in dict)
            {
                if (Properties.ContainsKey(name))
                {
                    Properties[name].SetForce(value);
                }
            }
        }

        public static ConfigContainer Create(Type type, ICoreAPI api, ILogger logger)
        {
            var attr = type.GetCustomAttribute<ConfigAttribute>() ?? throw new ArgumentException($"{type} is not a config");
            object inst = Activator.CreateInstance(type) ?? throw new ArgumentException($"Cant create object of {type}");

            var props = new Dictionary<string, ConfigPropertyData>();
            foreach (PropertyInfo prop in GetConfigProperties(type))
            {
                object? defaultValue = prop.GetValue(inst); // current instance is fresh, not modified
                props.Add(prop.Name, ConfigPropertyData.Create(inst, prop, defaultValue));
            }

            var container = new ConfigContainer(api, logger)
            {
                Type = type,
                Instance = inst,
                Attribute = attr
            };

            foreach (KeyValuePair<string, ConfigPropertyData> prop in props)
            {
                container.Properties.Add(prop.Key, prop.Value);
            }

            return container;
        }

        public static IEnumerable<PropertyInfo> GetConfigProperties(Type type)
        {
            var configAttr = type.GetCustomAttribute<ConfigAttribute>();

            if (configAttr == null)
            {
                yield break;
            }

            foreach (PropertyInfo prop in type.GetProperties())
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
    }
}
