
namespace UnarchivedStreamDownloader.Configuration.Models;

public class SearchSettings
{
    public required IReadOnlyCollection<string> ChannelIDs { get; init; }

    public required IReadOnlyCollection<string> Keywords { get; init; }
}
