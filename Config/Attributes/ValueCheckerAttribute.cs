using System;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    /// <summary>
    /// Base abstract value checker, use it for custom checkers
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ValueCheckerAttribute : Attribute
    {
        public abstract bool Check(ICoreAPI api, IComparable value);
        public abstract string GetDescription(ICoreAPI api);
    }
}
