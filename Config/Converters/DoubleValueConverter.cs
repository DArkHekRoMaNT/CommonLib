namespace CommonLib.Config
{
    public class DoubleValueConverter : IValueConverter
    {
        public object? Parse(string value)
        {
            if (double.TryParse(value, out double result))
            {
                return result;
            }
            return null;
        }
    }
}
