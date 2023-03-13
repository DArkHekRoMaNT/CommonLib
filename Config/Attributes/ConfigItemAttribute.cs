using System;
using System.Collections.Generic;

namespace CommonLib.Config
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ConfigItemAttribute : Attribute
    {
        public static Dictionary<Type, IValueConverter> DefaultConverters { get; } = new()
        {
            { typeof(int), new IntValueConverter() },
            { typeof(long), new LongValueConverter() },
            { typeof(float), new FloatValueConverter() },
            { typeof(double), new DoubleValueConverter() },
            { typeof(bool), new BoolValueConverter() },
            { typeof(string), new StringValueConverter() }
        };

        public Type Type { get; }
        public object DefaultValue { get; }
        public string? Description { get; set; }
        public bool ClientOnly { get; set; } = false;

        private object[]? _values;
        public object[]? Values
        {
            get => _values;
            set
            {
                _values = value;
                if (_values != null)
                {
                    for (int i = 0; i < _values.Length; i++)
                    {
                        _values[i] = Convert.ChangeType(_values[i], Type);
                    }
                }
            }
        }

        private object? _minValue;
        public object? MinValue
        {
            get => _minValue;
            set => _minValue = Convert.ChangeType(value, Type);
        }

        private object? _maxValue;
        public object? MaxValue
        {
            get => _maxValue;
            set => _maxValue = Convert.ChangeType(value, Type);
        }

        public IValueConverter? Converter { get; set; }

        public ConfigItemAttribute(Type type, object defaultValue)
        {
            Type = type;
            DefaultValue = Convert.ChangeType(defaultValue, Type);

            if (DefaultConverters.TryGetValue(type, out var converter))
            {
                Converter = converter;
            }
        }
    }
}
