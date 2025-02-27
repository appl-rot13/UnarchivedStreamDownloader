
namespace UnarchivedStreamDownloader.Core.Utilities.Extensions;

using System.Diagnostics.CodeAnalysis;

public static class ArrayExtensions
{
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this T[]? array)
    {
        return array == null || array.Length == 0;
    }

    public static bool IsAllTrue(this bool[] array)
    {
        //return array.All(element => element);
        foreach (var element in array.AsSpan())
        {
            if (!element)
            {
                return false;
            }
        }

        return true;
    }
}
