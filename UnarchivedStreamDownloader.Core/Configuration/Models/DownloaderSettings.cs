namespace UnarchivedStreamDownloader.Core.Configuration.Models;

public class DownloaderSettings
{
    public required string FilePath { get; init; }

    public IReadOnlyCollection<string> Options { get; init; } = [];
}
