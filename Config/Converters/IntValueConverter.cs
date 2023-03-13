namespace CommonLib.Config
{
    public class IntValueConverter : IValueConverter
    {
        public object? Parse(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return null;
        }
    }
}
