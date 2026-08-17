namespace UnarchivedStreamDownloader.YouTube;

using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.Core.YouTube;

public class YouTubeVideoSearcher(SearchSettings settings, IYouTubeFeedReader reader) : IYouTubeVideoSearcher
{
    public async IAsyncEnumerable<YouTubeVideo> EnumerateMatchingVideos(string channelId, bool suppressHttpErrors)
    {
        await foreach (var video in reader.EnumerateLatestVideos(channelId, suppressHttpErrors))
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
