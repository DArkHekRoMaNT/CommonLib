using System;
using System.Linq;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    /// <summary>
    /// Value is string from list
    /// </summary>
    public sealed class StringsAttribute : ConfigValueCheckerAttribute
    {
        private readonly string[] _strings;

        public StringsAttribute(params string[] strings)
        {
            _strings = strings;
        }

        public override bool IsValid(ICoreAPI api, object? value)
        {
            return value is string str && _strings.Contains(str);
        }

        public override string GetHelpDescription(ICoreAPI api)
        {
            return $"Allowed: {string.Join(", ", _strings)}";
        }
    }
}
