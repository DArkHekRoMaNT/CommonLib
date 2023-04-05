#if DEBUG

namespace CommonLib.Config
{
    [Config("test2.json")]
    public class TestConfig2
    {
        public int IntValue { get; set; } = 1;

        [Range(-1, 100)]
        public int IntValueRange { get; set; } = 2;

        [Range(-1, 100)]
        public long LongValueRange { get; set; } = 3;
    }
}
#endif
