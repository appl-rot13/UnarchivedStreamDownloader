namespace UnarchivedStreamDownloader.YouTube;

public interface IYouTubeVideoSource
{
    public IAsyncEnumerable<YouTubeVideo> EnumerateVideos(string channelId);
}
