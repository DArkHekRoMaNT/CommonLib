#if DEBUG

namespace CommonLib.Config
{
    [Config("test4.json", UseAllPropertiesByDefault = true)]
    public class TestConfig4
    {
        [ConfigIgnore]
        public int NotUseIt { get; set; } = 100;

        public int UseIt { get; set; } = 1;
    }
}
#endif
