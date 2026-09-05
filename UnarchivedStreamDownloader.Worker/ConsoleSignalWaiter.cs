namespace UnarchivedStreamDownloader.Worker;

public class ConsoleSignalWaiter : IConsoleSignalWaiter
{
    public async Task WaitForCancelKeyPressAsync(TimeSpan timeout, TimeProvider timeProvider)
    {
        using var signal = new ConsoleCancelKeyPressSignal();
        await signal.WaitAsync(timeout, timeProvider).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }
}
