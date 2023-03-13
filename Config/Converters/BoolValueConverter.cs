namespace CommonLib.Config
{
    public class BoolValueConverter : IValueConverter
    {
        public object? Parse(string value)
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            return null;
        }
    }
}
