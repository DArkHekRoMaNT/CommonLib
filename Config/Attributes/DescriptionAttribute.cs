using System;

namespace CommonLib.Config
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DescriptionAttribute : Attribute
    {
        public string Text { get; }

        public DescriptionAttribute(string text)
        {
            Text = text;
        }
    }
}
