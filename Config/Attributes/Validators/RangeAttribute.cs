using System;
using System.Linq;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    /// <summary>
    /// Value is numeric from min to max (including them)
    /// </summary>
    public sealed class RangeAttribute : ConfigValueCheckerAttribute
    {
        private readonly object _min;
        private readonly object _max;

        public Type Type { get; }

        public RangeAttribute(int min, int max)
        {
            Type = typeof(int);
            _min = min;
            _max = max;
        }

        public RangeAttribute(long min, long max)
        {
            Type = typeof(long);
            _min = min;
            _max = max;
        }

        public RangeAttribute(float min, float max)
        {
            Type = typeof(float);
            _min = min;
            _max = max;
        }

        public RangeAttribute(double min, double max)
        {
            Type = typeof(double);
            _min = min;
            _max = max;
        }

        public object GetMin(Type type) => Convert.ChangeType(_min, type);

        public object GetMax(Type type) => Convert.ChangeType(_max, type);

        public T GetMin<T>() => (T)GetMin(typeof(T));

        public T GetMax<T>() => (T)GetMax(typeof(T));

        public override bool IsValid(ICoreAPI api, object? value)
        {
            if (value == null)
            {
                return false;
            }

            var min = (IComparable)GetMin(value.GetType());
            var max = (IComparable)GetMax(value.GetType());

            return ((IComparable)value).CompareTo(min) >= 0 && ((IComparable)value).CompareTo(max) <= 0;
        }

        public override string GetHelpDescription(ICoreAPI api)
        {
            return $"{Type.Name} value from {_min} to {_max}";
        }
    }
}
