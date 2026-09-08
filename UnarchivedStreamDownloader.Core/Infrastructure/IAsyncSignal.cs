namespace UnarchivedStreamDownloader.Core.Infrastructure;

public interface IAsyncSignal : IDisposable
{
    bool IsTriggered { get; }

    Task WaitAsync(CancellationToken cancellationToken = default);

    Task WaitAsync(TimeSpan timeout, TimeProvider timeProvider);
}
