namespace CommonLib.Config
{
    public interface IValueConverter
    {
        object? Parse(string value);
    }
}
