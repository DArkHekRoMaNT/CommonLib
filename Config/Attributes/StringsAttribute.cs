using System;
using System.Linq;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    /// <summary>
    /// Value is string from list
    /// </summary>
    public sealed class StringsAttribute : ValueCheckerAttribute
    {
        private readonly string[] _strings;

        public StringsAttribute(params string[] strings)
        {
            _strings = strings;
        }

        public override bool Check(ICoreAPI api, IComparable value)
        {
            return _strings.Contains(value);
        }

        public override string GetDescription(ICoreAPI api)
        {
            return $"{string.Join(", ", _strings)}";
        }
    }
}
