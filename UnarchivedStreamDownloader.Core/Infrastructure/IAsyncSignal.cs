namespace UnarchivedStreamDownloader.Core.Infrastructure;

public interface IAsyncSignal : IDisposable
{
    bool IsSet { get; }

    Task WaitAsync(TimeSpan timeout, TimeProvider timeProvider);
}
