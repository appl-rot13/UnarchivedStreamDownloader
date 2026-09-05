namespace UnarchivedStreamDownloader.Worker;

public sealed class ConsoleCancelKeyPressSignal : IAsyncSignal
{
    private readonly TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ConsoleCancelKeyPressSignal()
    {
        Console.CancelKeyPress += this.OnCancelKeyPress;
    }

    public bool IsSet
    {
        get => this.tcs.Task.IsCompleted;
    }

    public Task WaitAsync(TimeSpan timeout, TimeProvider timeProvider)
    {
        return this.tcs.Task.WaitAsync(timeout, timeProvider);
    }

    public void Dispose()
    {
        Console.CancelKeyPress -= this.OnCancelKeyPress;
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        tcs.TrySetResult();
    }
}
