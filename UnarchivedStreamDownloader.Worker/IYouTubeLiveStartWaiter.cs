namespace UnarchivedStreamDownloader.Worker;

public interface IYouTubeLiveStartWaiter
{
    Task<bool> WaitForStartAsync(string videoId);
}
