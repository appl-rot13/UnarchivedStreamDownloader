namespace UnarchivedStreamDownloader.Worker;

public interface IVideoWaiter
{
    Task<bool> WaitAsync(string videoId);
}
