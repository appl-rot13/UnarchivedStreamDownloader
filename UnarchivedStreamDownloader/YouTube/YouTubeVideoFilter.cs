namespace UnarchivedStreamDownloader.YouTube;

using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.Core.YouTube;

public class YouTubeVideoFilter(SearchSettings settings, IYouTubeVideoSource source) : IYouTubeVideoSource
{
    public async IAsyncEnumerable<YouTubeVideo> EnumerateVideos(string channelId, bool suppressHttpErrors)
    {
        await foreach (var video in source.EnumerateVideos(channelId, suppressHttpErrors))
        {
            if (this.IsMatch(video))
            {
                yield return video;
            }
        }
    }

    private bool IsMatch(YouTubeVideo video)
    {
        if (settings.IgnoreVideoIDs.Contains(video.Id))
        {
            return false;
        }

        string[] sources = [video.Title, video.Description];
        return sources.Any(source => source.ContainsAny(settings.Keywords, StringComparison.OrdinalIgnoreCase));
    }
}
