using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    public sealed class WaypointNameAttribute : ValueCheckerAttribute
    {
        public override bool Check(ICoreAPI api, IComparable value)
        {
            return GetWaypointIcons(api).Any(e => e == (string)value);
        }

        public override string GetDescription(ICoreAPI api)
        {
            return $"Icons: {string.Join(", ", GetWaypointIcons(api))}";
        }

        private static string[] GetWaypointIcons(ICoreAPI api)
        {
            List<IAsset> icons = api.Assets.GetMany("textures/icons/worldmap/", null, loadAsset: false);
            return icons.Select(e =>
            {
                string name = e.Name.Substring(0, e.Name.IndexOf("."));
                name = Regex.Replace(name, "\\d+\\-", "");
                return name;
            }).ToArray();
        }
    }
}
