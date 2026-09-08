namespace UnarchivedStreamDownloader.Worker;

public interface IRetrySignalWaiter
{
    Task WaitAsync(TimeSpan timeout, TimeProvider timeProvider);
}
