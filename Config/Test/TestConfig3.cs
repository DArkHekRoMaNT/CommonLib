#if DEBUG

namespace CommonLib.Config
{
    [Config("test3.json", UseAllPropertiesByDefault = false)]
    public class TestConfig3
    {
        public int NotUseIt { get; set; } = 100;

        [ConfigProperty]
        public int UseIt { get; set; } = 1;
    }
}
#endif
