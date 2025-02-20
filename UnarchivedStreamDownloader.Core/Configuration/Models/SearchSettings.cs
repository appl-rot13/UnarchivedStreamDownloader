
namespace UnarchivedStreamDownloader.Core.Configuration.Models;

using UnarchivedStreamDownloader.Core.Utilities.Extensions;

public class SearchSettings
{
    public required IReadOnlyCollection<string> ChannelIDs { get; init; }

    public required IReadOnlyCollection<string> Keywords { get; init; }

    public bool IsMatch(string title, string description)
    {
        string[] sources = [title, description];
        return sources.Any(source => source.ContainsAny(this.Keywords, StringComparison.OrdinalIgnoreCase));
    }
}
