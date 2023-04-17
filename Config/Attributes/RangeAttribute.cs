using System;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    /// <summary>
    /// Value is numeric from min to max (including them)
    /// </summary>
    public sealed class RangeAttribute : ValueCheckerAttribute
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

        public override bool Check(ICoreAPI api, IComparable value)
        {
            var min = (IComparable)GetMin(value.GetType());
            var max = (IComparable)GetMax(value.GetType());
            return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
        }

        public object ClampRange(IComparable value)
        {
            // lower min value
            if (value.CompareTo(_min) < 0)
            {
                return _min;
            }

            // greater max value
            if (value.CompareTo(_max) > 0)
            {
                return _max;
            }

            return value;
        }

        public override string GetDescription(ICoreAPI api)
        {
            return $"{Type} value from {_min} to {_max}";
        }
    }
}
