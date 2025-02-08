
namespace UnarchivedStreamDownloader.Wrapper.Utilities.Extensions;

public static class StringExtensions
{
    public static string DoubleQuoted(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return $"\"{value}\"";
    }
}
