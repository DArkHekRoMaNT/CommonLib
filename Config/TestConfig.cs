#if DEBUG
namespace CommonLib.Config
{
    [Config("test.json")]
    public class TestConfig
    {
        [ConfigItem(typeof(int), 1)]
        public int IntValue { get; set; }

        [ConfigItem(typeof(int), 1, MinValue = -1, MaxValue = 100)]
        public int IntValueRange { get; set; }
    }

    [Config("test2.json")]
    public class TestConfig2
    {
        [ConfigItem(typeof(int), 1)]
        public int IntValue { get; set; }

        [ConfigItem(typeof(int), 1, MinValue = -1, MaxValue = 100)]
        public int IntValueRange { get; set; }

        [ConfigItem(typeof(long), 1, MinValue = -1, MaxValue = 100)]
        public long LongValueRange { get; set; }
    }
}
#endif
