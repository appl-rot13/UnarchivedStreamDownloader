namespace UnarchivedStreamDownloader.YouTube;

using UnarchivedStreamDownloader.Core.YouTube;

public interface IYouTubeFeedReader
{
    public IAsyncEnumerable<YouTubeVideo> EnumerateLatestVideos(string channelId, bool suppressHttpErrors);
}
