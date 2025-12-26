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
            if (value is not string code)
            {
                api.Logger.Error("PrivilegesAttribute.Check: expected string, got {0}.", value?.GetType().FullName ?? "null");
                return false;
            }
            return Privilege.AllCodes().Contains(code);
        }

        public override string GetDescription(ICoreAPI api)
        {
            return $"One of: {string.Join(", ", Privilege.AllCodes())}";
        }
    }
}
