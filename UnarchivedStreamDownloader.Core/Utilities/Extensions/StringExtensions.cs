
namespace UnarchivedStreamDownloader.Core.Utilities.Extensions;

public static class StringExtensions
{
    public static bool ContainsAny(
        this string source,
        IReadOnlyCollection<string> keywords,
        StringComparison comparisonType)
    {
        return keywords.Any(keyword => source.Contains(keyword, comparisonType));
    }
}
