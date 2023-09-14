using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace CommonLib.Config
{
    /// <summary>
    /// Value is any <see cref="Privilege"/>
    /// </summary>
    public sealed class PrivilegesAttribute : ConfigValueCheckerAttribute
    {
        public override bool IsValid(ICoreAPI api, object? value)
        {
            if (value is not string str)
            {
                return false;
            }

            return Privilege.AllCodes().Contains(str);
        }

        public override string GetHelpDescription(ICoreAPI api)
        {
            return $"One of: {string.Join(", ", Privilege.AllCodes())}";
        }
    }
}
