
namespace UnarchivedStreamDownloader.Core.Utilities.Extensions;

public static class EnumerableExtensions
{
    public static bool AllTrue(this IEnumerable<bool> source)
    {
        return source.All(element => element);
    }
}
