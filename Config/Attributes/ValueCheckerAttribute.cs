using System;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ValueCheckerAttribute : Attribute
    {
        public abstract bool Check(ICoreAPI api, IComparable value);
        public abstract string GetDescription(ICoreAPI api);
    }
}
