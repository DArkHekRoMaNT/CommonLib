using System;

namespace CommonLib.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RequiresRestartAttribute : Attribute { }
}