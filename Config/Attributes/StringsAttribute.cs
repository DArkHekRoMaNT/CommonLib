using System;
using System.Linq;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    /// <summary>
    /// Value is string from list
    /// </summary>
    public sealed class StringsAttribute(params string[] values) : ValueCheckerAttribute
    {
        public string[] Values { get; } = values;

        public override bool Check(ICoreAPI api, IComparable value)
        {
            return Values.Contains(value);
        }

        public override string GetDescription(ICoreAPI api)
        {
            return $"One of: {string.Join(", ", Values)}";
        }
    }
}
