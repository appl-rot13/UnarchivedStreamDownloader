namespace UnarchivedStreamDownloader.Worker;

public interface IConsoleSignalWaiter
{
    Task WaitForCancelKeyPressAsync(TimeSpan timeout, TimeProvider timeProvider);
}
