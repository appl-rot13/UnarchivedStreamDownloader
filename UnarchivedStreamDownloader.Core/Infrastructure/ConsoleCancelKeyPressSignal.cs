namespace UnarchivedStreamDownloader.Core.Infrastructure;

public class ConsoleCancelKeyPressSignal : EventSignal
{
    protected override void Subscribe()
    {
        Console.CancelKeyPress += this.OnCancelKeyPress;
    }

    protected override void Unsubscribe()
    {
        Console.CancelKeyPress -= this.OnCancelKeyPress;
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        this.Trigger();
    }
}
