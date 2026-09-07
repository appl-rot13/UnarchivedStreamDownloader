namespace UnarchivedStreamDownloader.Core.Utilities.Extensions;

public static class StringExtensions
{
    public static bool ContainsAny(
        this string text,
        IEnumerable<string> keywords,
        StringComparison comparisonType)
    {
        return keywords.Any(keyword => text.Contains(keyword, comparisonType));
    }

    public static IEnumerable<string> ExcludeEmptyOrWhitespace(this IEnumerable<string> source)
    {
        return source.Where(value => !string.IsNullOrWhiteSpace(value));
    }
}
