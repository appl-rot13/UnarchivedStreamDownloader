
namespace UnarchivedStreamDownloader.Configuration.Models;

public class AppSettings
{
    public required DownloaderSettings DownloaderSettings { get; init; }

    public required SearchSettings SearchSettings { get; init; }
}
