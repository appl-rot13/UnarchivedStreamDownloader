
namespace UnarchivedStreamDownloader.YouTube;

public record YouTubeChannel(
    string Id,
    string Name);

public record YouTubeVideo(
    YouTubeChannel Channel,
    string Id,
    string Title,
    string Description);
