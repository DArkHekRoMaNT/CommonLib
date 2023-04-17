using System;

namespace CommonLib.Config
{
    /// <summary>
    /// This value will not be synchronized from server to client
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ClientOnlyAttribute : Attribute { }
}
