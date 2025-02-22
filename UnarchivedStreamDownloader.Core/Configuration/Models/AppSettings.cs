
namespace UnarchivedStreamDownloader.Core.Configuration.Models;

public class AppSettings
{
    public required DownloaderSettings DownloaderSettings { get; init; }

    public required BehaviorSettings BehaviorSettings { get; init; }

    public required SearchSettings SearchSettings { get; init; }
}
