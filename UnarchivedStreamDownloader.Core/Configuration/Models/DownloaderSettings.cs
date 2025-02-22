
namespace UnarchivedStreamDownloader.Core.Configuration.Models;

public class DownloaderSettings
{
    public required string FilePath { get; init; }

    public required IReadOnlyCollection<string> Options { get; init; }
}
