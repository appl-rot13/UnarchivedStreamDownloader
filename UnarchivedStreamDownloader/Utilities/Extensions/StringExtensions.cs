
namespace UnarchivedStreamDownloader.Utilities.Extensions;

public static class StringExtensions
{
    public static bool ContainsAny(
        this string source,
        IReadOnlyCollection<string> keywords,
        StringComparison comparisonType)
    {
        return keywords.Any(keyword => source.Contains(keyword, comparisonType));
    }

    public static string DoubleQuoted(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return $"\"{value}\"";
    }
}
