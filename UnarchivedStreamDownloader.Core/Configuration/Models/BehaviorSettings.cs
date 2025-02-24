
namespace UnarchivedStreamDownloader.Core.Configuration.Models;

public class BehaviorSettings
{
    public required int DownloadAttempts { get; init; }

    public required int ErrorRetryAttempts { get; init; }

    public required double ErrorRetryIntervalSeconds
    {
        get => this.ErrorRetryInterval.TotalSeconds;
        init => this.ErrorRetryInterval = TimeSpan.FromSeconds(value);
    }

    public required double StartCheckBufferSeconds
    {
        get => this.StartCheckBuffer.TotalSeconds;
        init => this.StartCheckBuffer = TimeSpan.FromSeconds(value);
    }

    public required double StartCheckIntervalSeconds
    {
        get => this.StartCheckInterval.TotalSeconds;
        init => this.StartCheckInterval = TimeSpan.FromSeconds(value);
    }

    public TimeSpan ErrorRetryInterval { get; private init; }

    public TimeSpan StartCheckBuffer { get; private init; }

    public TimeSpan StartCheckInterval { get; private init; }
}
