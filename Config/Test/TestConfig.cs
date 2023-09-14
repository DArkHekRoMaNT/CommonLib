#if DEBUG

namespace CommonLib.Config
{
    [Config("test.json")]
    public class TestConfig
    {
        public int IntValue { get; set; } = 50;

        [Range(-1, 100)]
        [Description("my desc")]
        public int IntValueRange { get; set; } = 1;
    }
}
#endif
