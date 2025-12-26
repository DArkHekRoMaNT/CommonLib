using System;
using System.Globalization;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    /// <summary>
    /// Value is hex color string, for example: #112233
    /// </summary>
    public sealed class HexColorAttribute : ValueCheckerAttribute
    {
        public override bool Check(ICoreAPI api, IComparable value)
        {
            if (value is not string hexStr)
            {
                api.Logger.Error("HexColorAttribute.Check: expected string, got {0}.", value?.GetType().FullName ?? "null");
                return false;
            }

            if (!hexStr.StartsWith("#") || hexStr.Length != 7)
            {
                return false;
            }

            return int.TryParse(hexStr.Substring(1), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out _);
        }

        public override string GetDescription(ICoreAPI api)
        {
            return $"Hex color, for example: #112233";
        }
    }
}
