using System;
using System.Collections.Generic;
using CommonLib.Utils;
using Vintagestory.API.MathTools;

namespace CommonLib.Extensions
{
    public static class RandomExtensions
    {
        public static T GetItem<T>(this LCGRandom random, IList<T> values)
        {
            if (random == null)
            {
                var message = $"RandomExtensions.GetItem: random is null. Type={typeof(T).FullName ?? typeof(T).Name}";
                CommonLibLogger.Error(message);
                throw new ArgumentNullException(nameof(random), message);
            }
            if (values == null)
            {
                var message = $"RandomExtensions.GetItem: values is null. Type={typeof(T).FullName ?? typeof(T).Name}";
                CommonLibLogger.Error(message);
                throw new ArgumentNullException(nameof(values), message);
            }
            if (values.Count == 0)
            {
                var message = $"RandomExtensions.GetItem: values is empty. Type={typeof(T).FullName ?? typeof(T).Name}";
                CommonLibLogger.Error(message);
                throw new ArgumentException(message, nameof(values));
            }
            return values[random.NextInt(values.Count)];
        }
    }
}
