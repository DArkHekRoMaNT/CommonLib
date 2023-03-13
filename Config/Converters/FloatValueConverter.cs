namespace CommonLib.Config
{
    public class FloatValueConverter : IValueConverter
    {
        public object? Parse(string value)
        {
            if (float.TryParse(value, out float result))
            {
                return result;
            }
            return null;
        }
    }
}
