using System;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    /// <summary>
    /// Base abstract config value converter, use it for custom value presets
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ConfigValueCheckerAttribute : Attribute
    {
        public abstract bool IsValid(ICoreAPI api, object? value);

        public abstract string GetHelpDescription(ICoreAPI api);
    }
}
