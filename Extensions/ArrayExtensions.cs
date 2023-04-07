using Vintagestory.API.Util;

namespace CommonLib.Extensions
{
    public static class ArrayExtensions
    {
        /// <summary>
        /// Creates a new copy of the array with value append to the end of the array
        /// if condition is true
        /// </summary>
        public static T[] AppendIf<T>(this T[] array, bool condition, params T[] value)
        {
            if (condition)
            {
                return array.Append(value);
            }

            return array;
        }
    }
}
