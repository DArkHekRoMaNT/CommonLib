using System;
using System.Globalization;

namespace CommonLib.Config
{
    public sealed class HexColorAttribute : ValueCheckerAttribute
    {
        public override bool Check(IComparable value)
        {
            string hexStr = (string)value;

            if (!hexStr.StartsWith("#") && hexStr.Length != 7)
            {
                return false;
            }

            return int.TryParse(hexStr.Substring(1), NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out _);
        }

        public override string GetDescription()
        {
            return $"Hex color, for example: #112233";
        }
    }
}
