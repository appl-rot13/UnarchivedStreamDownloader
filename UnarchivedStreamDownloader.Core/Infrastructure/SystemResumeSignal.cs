namespace UnarchivedStreamDownloader.Core.Infrastructure;

using Microsoft.Win32;

public class SystemResumeSignal : EventSignal
{
    protected override void Subscribe()
    {
        SystemEvents.PowerModeChanged += this.OnPowerModeChanged;
    }

    protected override void Unsubscribe()
    {
        SystemEvents.PowerModeChanged -= this.OnPowerModeChanged;
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            this.Trigger();
        }
    }
}
