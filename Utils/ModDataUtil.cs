using System;
using System.IO;
using Vintagestory.API.Common;

namespace CommonLib.Utils
{
    public static class ModDataUtil
    {
        public static T LoadOrCreateConfig<T>(this ICoreAPI api, string file, ILogger logger, T? defaultConfig = null) where T : class, new()
        {
            logger ??= api.Logger;
            T? config = defaultConfig;

            try
            {
                config = api.LoadModConfig<T>(file);
            }
            catch (Exception e)
            {
                logger.Error("Failed loading file ({0}), error {1}. Will initialize new one", file, e);
            }

            config ??= new();
            api.StoreModConfig(config, file);
            return config;
        }

        public static T? LoadDataFile<T>(this ICoreAPI api, string file, ILogger logger)
        {
            logger ??= api.Logger;

            try
            {
                if (File.Exists(file))
                {
                    var content = File.ReadAllText(file);
                    return JsonUtil.FromString<T>(content);
                }
            }
            catch (Exception e)
            {
                logger.Error("Failed loading file ({0}), error {1}", file, e);
            }

            return default;
        }

        public static T LoadOrCreateDataFile<T>(this ICoreAPI api, string file, ILogger logger) where T : class, new()
        {
            logger ??= api.Logger;

            var data = api.LoadDataFile<T>(file, logger);
            if (data == null)
            {
                logger.Notification("Will initialize new data file");
                data = new();
                SaveDataFile(api, file, data, logger);
            }

            return data;
        }

        public static void SaveDataFile<T>(this ICoreAPI api, string file, T data, ILogger logger)
        {
            logger ??= api.Logger;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                var content = JsonUtil.ToString(data);
                File.WriteAllText(file, content);
            }
            catch (Exception e)
            {
                logger.Error("Failed saving file ({0}), error {1}", file, e);
            }
        }
    }
}
