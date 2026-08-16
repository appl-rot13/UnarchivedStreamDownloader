
namespace UnarchivedStreamDownloader.YouTube;

using UnarchivedStreamDownloader.Core.YouTube;

public interface IYouTubeVideoSearcher
{
    public IAsyncEnumerable<YouTubeVideo> EnumerateMatchingVideos(string channelId, bool suppressHttpErrors);
}
