
namespace UnarchivedStreamDownloader.Core.Utilities.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> ExcludeNull<T>(this IEnumerable<T?> source)
        where T : struct
    {
        return source.Where(element => element.HasValue).Select(element => element!.Value);
    }
}
