using System;

namespace CommonLib.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ValueCheckerAttribute : Attribute
    {
        public abstract bool Check(IComparable value);
        public abstract string GetDescription();
    }
}
