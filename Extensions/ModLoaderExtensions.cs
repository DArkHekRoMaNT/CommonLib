using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace CommonLib.Extensions
{
    public static class ModLoaderExtensions
    {
        public static bool IsModEnabled(this IModLoader modLoader, Assembly assembly)
        {
            Mod? mod = GetMod(modLoader, assembly);
            return mod != null && modLoader.IsModEnabled(mod.Info.ModID);
        }

        public static Mod? GetMod(this IModLoader modLoader, Assembly assembly)
        {
            return modLoader.Mods.FirstOrDefault(e => (e as ModContainer)?.Assembly == assembly);
        }
    }
}
