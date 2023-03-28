using System;
using System.Linq;
using Vintagestory.API.Server;

namespace CommonLib.Config
{
    public sealed class PrivilegesAttribute : ValueCheckerAttribute
    {
        public override bool Check(IComparable value)
        {
            return Privilege.AllCodes().Contains((string)value);
        }

        public override string GetDescription()
        {
            return $"One of: {string.Join(", ", Privilege.AllCodes())}";
        }
    }
}
