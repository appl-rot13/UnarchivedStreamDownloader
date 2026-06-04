
namespace UnarchivedStreamDownloader.Core.Utilities.Extensions;

public static class TaskExtensions
{
    public static Task WhenAll(this IEnumerable<Task> tasks)
    {
        return Task.WhenAll(tasks);
    }

    public static Task<TResult[]> WhenAll<TResult>(this IEnumerable<Task<TResult>> tasks)
    {
        return Task.WhenAll(tasks);
    }
}
