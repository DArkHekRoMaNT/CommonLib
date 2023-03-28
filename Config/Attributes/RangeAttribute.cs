using System;

namespace CommonLib.Config
{
    public sealed class RangeAttribute : ValueCheckerAttribute
    {
        public Type Type { get; }
        public IComparable Min { get; }
        public IComparable Max { get; }

        public RangeAttribute(int min, int max)
        {
            Type = typeof(int);
            Min = min;
            Max = max;
        }

        public RangeAttribute(long min, long max)
        {
            Type = typeof(long);
            Min = min;
            Max = max;
        }

        public RangeAttribute(float min, float max)
        {
            Type = typeof(float);
            Min = min;
            Max = max;
        }

        public RangeAttribute(double min, double max)
        {
            Type = typeof(double);
            Min = min;
            Max = max;
        }

        public override bool Check(IComparable value)
        {
            var min = (IComparable)Convert.ChangeType(Min, value.GetType());
            var max = (IComparable)Convert.ChangeType(Max, value.GetType());
            return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
        }

        public object ClampRange(IComparable value)
        {
            // lower min value
            if (value.CompareTo(Min) < 0)
            {
                return Min;
            }

            // greater max value
            if (value.CompareTo(Max) > 0)
            {
                return Max;
            }

            return value;
        }

        public override string GetDescription()
        {
            return $"{Type} value from {Min} to {Max}";
        }
    }
}
