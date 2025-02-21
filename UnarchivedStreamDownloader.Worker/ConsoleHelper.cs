
namespace UnarchivedStreamDownloader.Worker;

public static class ConsoleHelper
{
    public static async Task WaitForCancelKeyPress(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();

        ConsoleCancelEventHandler? handler = null;
        handler = (_, e) =>
        {
            Console.CancelKeyPress -= handler;
            tcs.SetResult();

            e.Cancel = true;
        };

        await using var _ = cancellationToken.Register(() =>
        {
            Console.CancelKeyPress -= handler;
            tcs.TrySetCanceled();
        });

        Console.CancelKeyPress += handler;
        await tcs.Task;
    }
}
