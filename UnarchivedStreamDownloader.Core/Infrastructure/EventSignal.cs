namespace UnarchivedStreamDownloader.Core.Infrastructure;

public abstract class EventSignal : IAsyncSignal
{
    private readonly TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected EventSignal()
    {
        this.Subscribe();
    }

    public bool IsTriggered
    {
        get => this.tcs.Task.IsCompleted;
    }

    public Task WaitAsync(CancellationToken cancellationToken = default)
    {
        return this.tcs.Task.WaitAsync(cancellationToken);
    }

    public Task WaitAsync(TimeSpan timeout, TimeProvider timeProvider)
    {
        return this.tcs.Task.WaitAsync(timeout, timeProvider);
    }

    public void Dispose()
    {
        this.Unsubscribe();
        this.tcs.TrySetCanceled();

        GC.SuppressFinalize(this);
    }

    protected void Trigger()
    {
        this.tcs.TrySetResult();
    }

    protected abstract void Subscribe();

    protected abstract void Unsubscribe();
}
