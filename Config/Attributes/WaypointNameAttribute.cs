using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace CommonLib.Config
{
    public sealed class WaypointNameAttribute : ValueCheckerAttribute
    {
        private WaypointMapLayer? _waypointMapLayer;

        public override void Init(ICoreAPI api)
        {
            var mapLayers = api.ModLoader.GetModSystem<WorldMapManager>().MapLayers;
            _waypointMapLayer = (WaypointMapLayer)mapLayers.FirstOrDefault(e => e is WaypointMapLayer);
        }

        public override bool Check(IComparable value)
        {
            if (_waypointMapLayer is null)
            {
                throw new InvalidOperationException("Not inited");
            }

            return _waypointMapLayer.WaypointIcons.Any(e => e.Key == (string)value);
        }

        public override string GetDescription()
        {
            if (_waypointMapLayer is null)
            {
                throw new InvalidOperationException("Not inited");
            }

            string[] icons = _waypointMapLayer.WaypointIcons.Select(e => e.Key).ToArray();
            return $"Icons: {string.Join(", ", icons)}";
        }
    }
}
