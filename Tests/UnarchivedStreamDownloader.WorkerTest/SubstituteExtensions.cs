namespace UnarchivedStreamDownloader.WorkerTest;

using NSubstitute;
using NSubstitute.Core;

public static class SubstituteExtensions
{
    public static ConfiguredCall Returns<T>(this T value, params T[] returnThese)
    {
        return value.Returns(returnThese[0], returnThese[1..]);
    }

    public static ConfiguredCall Returns<T>(this T value, params IReadOnlyList<T> returnThese)
    {
        return value.Returns(returnThese[0], [.. returnThese.Skip(1)]);
    }

    public static ConfiguredCall Returns<T>(this Task<T> value, params T[] returnThese)
    {
        return value.Returns(returnThese[0], returnThese[1..]);
    }

    public static ConfiguredCall Returns<T>(this Task<T> value, params IReadOnlyList<T> returnThese)
    {
        return value.Returns(returnThese[0], [.. returnThese.Skip(1)]);
    }
}
