namespace UnarchivedStreamDownloader.Core.Utilities.Extensions;

using System.Runtime.CompilerServices;

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

    public static ConfiguredTaskAwaitable SuppressThrowing(this Task task)
    {
        return task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
}
