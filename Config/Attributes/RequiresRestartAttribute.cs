using System;

namespace CommonLib.Config
{
    /// <summary>
    /// For changes to this parameter to take effect, a restart will be required
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RequiresRestartAttribute : Attribute { }
}
