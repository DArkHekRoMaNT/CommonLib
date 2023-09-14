using System;
using System.Globalization;
using System.Linq;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    /// <summary>
    /// Value is hex color string, for example: #112233
    /// </summary>
    public sealed class HexColorAttribute : ConfigValueCheckerAttribute
    {
        public override bool IsValid(ICoreAPI api, object? value)
        {
            if (value is not string hexStr)
            {
                return false;
            }

            if (!hexStr.StartsWith("#") || hexStr.Length != 7)
            {
                return false;
            }

            return int.TryParse(hexStr.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
        }

        public override string GetHelpDescription(ICoreAPI api)
        {
            return "Hex color, for example: #112233";
        }
    }
}
