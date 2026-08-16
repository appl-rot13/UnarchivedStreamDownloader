
namespace UnarchivedStreamDownloader.YouTube;

using UnarchivedStreamDownloader.Core.YouTube;

public interface IYouTubeVideoDownloader
{
    public Task<bool?> DownloadAsync(YouTubeVideo video);

    public bool? Download(YouTubeVideo video);
}
