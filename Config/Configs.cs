using CommonLib.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    public sealed class Configs : IEnumerable<ConfigContainer>
    {
        private readonly Dictionary<Type, ConfigContainer?> _configs = new();

        private readonly ICoreAPI _api;
        private readonly ILogger _logger;

        public IList<Type> Errors { get; } = new List<Type>();

        public Configs(ICoreAPI api, ILogger logger)
        {
            _api = api;
            _logger = logger;
        }

        public ConfigContainer this[Type key]
        {
            get
            {
                if (Errors.Contains(key))
                {
                    throw new InvalidOperationException($"Try access broken config type {key.AssemblyQualifiedName} on side {_api.Side}. Check logs {_api.Side.ToString().ToLower()}-main.txt");
                }

                if (!_configs.TryGetValue(key, out ConfigContainer? container))
                {
                    throw new InvalidOperationException($"Unknown config type {key.AssemblyQualifiedName} on side {_api.Side}");
                }

                if (container == null)
                {
                    throw new UnreachableException($"Not handled access to config type {key.AssemblyQualifiedName} on side {_api.Side}");
                }

                return container;
            }
        }

        public T GetConfig<T>() where T : class
        {
            return (T)this[typeof(T)].Instance;
        }

        public void LoadAll()
        {
            foreach (Type type in GetAllConfigTypes())
            {
                if (!_api.ModLoader.IsModEnabled(type.Assembly))
                {
                    _logger.Notification($"Сonfig {type.AssemblyQualifiedName} skipped (disabled mod)");
                    continue;
                }

                LoadConfig(type);
            }
        }

        private void LoadConfig(Type type)
        {
            try
            {
                var container = ConfigContainer.Create(type, _api, _logger);
                container.LoadOrCreate();
                _configs.Add(type, container);
                _logger.Notification($"Config {type.AssemblyQualifiedName} loaded successfully");
            }
            catch (Exception e)
            {
                Errors.Add(type);
                _logger.Error($"Take error during load config {type.AssemblyQualifiedName}, skipped. " +
                    $"May cause problems with this mod further. Error:\n{e}");
            }
        }

        private IEnumerable<Type> GetAllConfigTypes()
        {
            var configs = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var assemblyConfigs = new List<Type>();
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (Attribute.IsDefined(type, typeof(ConfigAttribute)))
                        {
                            assemblyConfigs.Add(type);
                        }
                    }
                    configs.AddRange(assemblyConfigs);
                }
                catch (FileNotFoundException) { }
                catch (TypeLoadException) { }
                catch (Exception e)
                {
                    _logger.Warning($"Assembly {assembly.FullName} skipped. {e.GetType()}: {e.Message}");
                }
            }

            return configs;
        }

        public IEnumerator<ConfigContainer> GetEnumerator()
        {
            return _configs.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _configs.Values.GetEnumerator();
        }
    }
}
