
namespace UnarchivedStreamDownloader.Core.Configuration.Models;

using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.Core.YouTube;

public class SearchSettings
{
    public required IReadOnlyCollection<string> ChannelIDs { get; init; }

    public required IReadOnlyCollection<string> IgnoreVideoIDs { get; init; }

    public required IReadOnlyCollection<string> Keywords { get; init; }

    public bool IsMatch(YouTubeVideo video)
    {
        if (this.IgnoreVideoIDs.Contains(video.Id))
        {
            return false;
        }

        string[] sources = [video.Title, video.Description];
        return sources.Any(source => source.ContainsAny(this.Keywords, StringComparison.OrdinalIgnoreCase));
    }
}
