using System;
using System.Globalization;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace CommonLib.Config
{
    public sealed class StringsAttribute : ValueCheckerAttribute
    {
        private readonly string[] _strings;

        public StringsAttribute(params string[] strings)
        {
            _strings = strings;
        }

        public override bool Check(IComparable value)
        {
            return _strings.Contains(value);
        }

        public override string GetDescription()
        {
            return $"String values: {string.Join(", ", _strings)}";
        }
    }
}
