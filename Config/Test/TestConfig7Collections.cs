#if DEBUG
using System.Collections.Generic;

namespace CommonLib.Config
{
    [Config("test7collections.json")]
    public class TestConfig7Collections
    {
        public List<int> IntList { get; set; } = new() { 1, 2, 3, 4 };
        public List<string> StringList { get; set; } = new() { "1", "2", "3", "4" };
        public IList<int> IntIList { get; set; } = new List<int>() { 1, 2, 3, 4 };
        public HashSet<int> IntHashSet { get; set; } = new() { 1, 2, 3, 4 };
        public Dictionary<string, int> IntStringDict { get; set; } = new() { { "1", 1 }, { "2", 2 }, { "3", 3 } };
        public List<List<int>> IntListList { get; set; } = new() { new() { 1, 2, 3 }, new() { 4, 5, 6 }, new() { 7, 8, 9 } };
    }
}
#endif
