using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace CommonLib.Config
{
    public class WaypointIconNameAttribute : ConfigValueCheckerAttribute
    {
        public override bool IsValid(ICoreAPI api, object? value)
        {
            return value is string str && GetWaypointIcons(api).Any(e => e == str);
        }

        public override string GetHelpDescription(ICoreAPI api)
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
