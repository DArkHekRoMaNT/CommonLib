namespace CommonLib.Config
{
    public class LongValueConverter : IValueConverter
    {
        public object? Parse(string value)
        {
            if (long.TryParse(value, out long result))
            {
                return result;
            }
            return null;
        }
    }
}
