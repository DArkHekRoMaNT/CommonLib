#if DEBUG

namespace CommonLib.Config
{
    [Config("test6.json")]
    public class TestConfig6
    {
        [WaypointIconName]
        public string WaypointName { get; set; } = "circle";

        [HexColor]
        public string HexColor { get; set; } = "#121314";
    }
}
#endif
