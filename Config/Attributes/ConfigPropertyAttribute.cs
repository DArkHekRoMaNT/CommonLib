using System;

namespace CommonLib.Config
{    /// <summary>
     /// This property will be added to config
     /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigPropertyAttribute : Attribute { }
}
