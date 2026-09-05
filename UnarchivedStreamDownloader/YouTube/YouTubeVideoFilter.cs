namespace UnarchivedStreamDownloader.YouTube;

using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.Core.YouTube;

public class YouTubeVideoFilter(SearchSettings settings, IYouTubeVideoSource source) : IYouTubeVideoSource
{
    private readonly IReadOnlyCollection<string> ignoreVideoIDs = Normalize(settings.IgnoreVideoIDs, true);
    private readonly IReadOnlyCollection<string> keywords = Normalize(settings.Keywords, false);

    public async IAsyncEnumerable<YouTubeVideo> EnumerateVideos(string channelId)
    {
        await foreach (var video in source.EnumerateVideos(channelId))
        {
            if (this.IsMatch(video))
            {
                yield return video;
            }
        }
    }

    private bool IsMatch(YouTubeVideo video)
    {
        if (ignoreVideoIDs.Contains(video.Id))
        {
            return false;
        }

        string[] sources = [video.Title, video.Description];
        return sources.Any(source => source.ContainsAny(keywords, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyCollection<string> Normalize(IEnumerable<string> source, bool trim)
    {
        source = source.ExcludeEmptyOrWhitespace();
        if (trim)
        {
            source = source.Select(value => value.Trim());
        }

        source = source.Distinct();
        return [.. source];
    }
}
