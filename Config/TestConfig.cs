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

    [Config("test2.json")]
    public class TestConfig2
    {
        public int IntValue { get; set; } = 1;

        [Range(-1, 100)]
        public int IntValueRange { get; set; } = 2;

        [Range(-1, 100)]
        public long LongValueRange { get; set; } = 3;
    }

    [Config("test3.json", UseAllPropertiesByDefault = false)]
    public class TestConfig3
    {
        public int NotUseIt { get; set; } = 100;

        [ConfigProperty]
        public int UseIt { get; set; } = 1;
    }

    [Config("test4.json", UseAllPropertiesByDefault = true)]
    public class TestConfig4
    {
        [ConfigIgnore]
        public int NotUseIt { get; set; } = 100;

        public int UseIt { get; set; } = 1;
    }
}
#endif
