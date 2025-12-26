using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    public sealed class WaypointNameAttribute : ValueCheckerAttribute
    {
        private static readonly Regex WaypointNumberPrefixRegex = new Regex("\\d+\\-", RegexOptions.Compiled);

        public override bool Check(ICoreAPI api, IComparable value)
        {
            if (value is not string iconName)
            {
                api.Logger.Error("WaypointNameAttribute.Check: expected string, got {0}.", value?.GetType().FullName ?? "null");
                return false;
            }
            return GetWaypointIcons(api).Any(e => e == iconName);
        }

        public override string GetDescription(ICoreAPI api)
        {
            return $"Icons: {string.Join(", ", GetWaypointIcons(api))}";
        }

        private static string[] GetWaypointIcons(ICoreAPI api)
        {
            List<IAsset> icons = api.Assets.GetMany("textures/icons/worldmap/", null, loadAsset: false) ?? [];
            var names = new List<string>(icons.Count);
            foreach (var icon in icons)
            {
                string name = icon.Name;
                int dotIndex = name.IndexOf(".");
                if (dotIndex <= 0)
                {
                    api.Logger.Error("WaypointNameAttribute.GetWaypointIcons: invalid asset name '{0}' (missing extension).", name);
                    continue;
                }
                name = name.Substring(0, dotIndex);
                name = WaypointNumberPrefixRegex.Replace(name, "");
                names.Add(name);
            }
            return names.ToArray();
        }
    }
}
