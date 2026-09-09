namespace UnarchivedStreamDownloader.YouTube;

using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;

public class YouTubeVideoFilter(SearchSettings settings, IYouTubeVideoSource source) : IYouTubeVideoSource
{
    private readonly IReadOnlyCollection<string> ignoreVideoIds = Normalize(settings.IgnoreVideoIDs, true);
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
        if (this.ignoreVideoIds.Contains(video.Id))
        {
            return false;
        }

        string[] texts = [video.Title, video.Description];
        return texts.Any(text => text.ContainsAny(this.keywords, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyCollection<string> Normalize(IEnumerable<string> values, bool trim)
    {
        values = values.ExcludeEmptyOrWhitespace();
        if (trim)
        {
            values = values.Select(value => value.Trim());
        }

        values = values.Distinct();
        return [.. values];
    }
}
