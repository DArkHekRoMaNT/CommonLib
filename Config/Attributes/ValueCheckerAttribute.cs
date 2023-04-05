using System;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ValueCheckerAttribute : Attribute
    {
        public virtual void Init(ICoreAPI api) { }
        public abstract bool Check(IComparable value);
        public abstract string GetDescription();
    }
}
