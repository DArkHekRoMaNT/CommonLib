using System;

namespace CommonLib.Config
{
    /// <summary>
    /// Specifies the config class. Requires parameterless ctor.
    /// Config items are set with <see cref="ConfigValueAttribute"/>
    /// and excluded with <see cref="ConfigIgnoreAttribute"/>
    /// (if <see cref="UseAllPropertiesByDefault"/>t is true).
    /// See <see cref="TestConfig"/> for more info
    /// </summary>
    /// <example>
    /// <code>
    /// [Config("myconfig.json")]
    /// public class MyConfig
    /// {
    ///     [ConfigValue]
    ///     public bool MyToggle { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigAttribute : Attribute
    {
        public string Filename { get; }
        public bool UseAllPropertiesByDefault { get; set; } = true;

        public ConfigAttribute(string filename)
        {
            Filename = filename;
        }
    }
}
