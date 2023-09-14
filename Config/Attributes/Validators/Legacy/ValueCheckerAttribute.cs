using System;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    [Obsolete($"Use {nameof(ConfigValueCheckerAttribute)} instead", true)]
    public abstract class ValueCheckerAttribute : ConfigValueCheckerAttribute
    {
        public abstract bool Check(ICoreAPI api, IComparable value);

        public override bool IsValid(ICoreAPI api, object? value)
        {
            return value is IComparable comparableValue && Check(api, comparableValue);
        }
    }
}
