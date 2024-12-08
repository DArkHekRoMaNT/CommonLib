#if DEBUG
namespace CommonLib.Config
{
    [Config("test7.json")]
    public class TestConfig7
    {
        public enum TestValues
        {
            Value1,
            Value2,
            Value3,
            LoremIpsumDolor
        }

        public TestValues Enums { get; set; } = TestValues.Value3;

        [Strings("Value1", "Value2", "Value3", "LoremIpsumDolor")]
        public string Strings { get; set; } = "Value2";
    }
}
#endif
