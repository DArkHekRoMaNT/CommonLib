using System;

namespace CommonLib.Config
{
    /// <summary>
    /// This property will be ignored
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigIgnoreAttribute : Attribute { }
}
