using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib.Config
{
    /// <summary>
    /// Value is any <see cref="Privilege"/>
    /// </summary>
    public sealed class PrivilegesAttribute : ValueCheckerAttribute
    {
        public override bool Check(ICoreAPI api, IComparable value)
        {
            return Privilege.AllCodes().Contains((string)value);
        }

        public override string GetDescription(ICoreAPI api)
        {
            return $"One of: {string.Join(", ", Privilege.AllCodes())}";
        }
    }
}
