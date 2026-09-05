namespace UnarchivedStreamDownloader.Core.Configuration.Models;

public class SearchSettings
{
    public IReadOnlyCollection<string> ChannelIDs { get; init; } = [];

    public IReadOnlyCollection<string> IgnoreVideoIDs { get; init; } = [];

    public IReadOnlyCollection<string> Keywords { get; init; } = [];
}
