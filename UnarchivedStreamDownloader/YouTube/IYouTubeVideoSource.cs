namespace UnarchivedStreamDownloader.YouTube;

using UnarchivedStreamDownloader.Core.YouTube;

public interface IYouTubeVideoSource
{
    public IAsyncEnumerable<YouTubeVideo> EnumerateVideos(string channelId, bool suppressHttpErrors);
}
