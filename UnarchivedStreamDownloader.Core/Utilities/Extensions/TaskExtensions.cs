
namespace UnarchivedStreamDownloader.Core.Utilities.Extensions;

public static class TaskExtensions
{
    public static Task<TResult[]> WhenAll<TResult>(this IEnumerable<Task<TResult>> tasks)
    {
        return Task.WhenAll(tasks);
    }
}
