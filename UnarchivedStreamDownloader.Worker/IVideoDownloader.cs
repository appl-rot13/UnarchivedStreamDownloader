namespace UnarchivedStreamDownloader.Worker;

public interface IVideoDownloader
{
    Task<string> GetVideoDetailsAsync(string videoId);

    Task<bool> DownloadAsync(string videoId);

    bool ArchiveFileExists(string videoId);
}
