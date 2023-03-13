using System;

namespace CommonLib.Config
{
    /// <example>
    /// <code>
    /// [Config("myconfig.json")]
    /// public class MyConfig
    /// {
    ///     [ConfigItem(typeof(bool),
    ///         true,
    ///         Description = "My desc"]
    ///     public bool MyToggle { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ConfigAttribute : Attribute
    {
        public string Filename { get; }

        public ConfigAttribute(string filename)
        {
            Filename = filename;
        }
    }
}
