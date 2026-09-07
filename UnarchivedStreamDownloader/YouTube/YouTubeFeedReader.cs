namespace UnarchivedStreamDownloader.YouTube;

using System.Xml.Linq;
using UnarchivedStreamDownloader.Core.Infrastructure;

public class YouTubeFeedReader(IHttpReader httpReader, bool suppressHttpErrors) : IYouTubeVideoSource
{
    public static string GetFeedUrl(string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId, nameof(channelId));
        return $"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}";
    }

    public async IAsyncEnumerable<YouTubeVideo> EnumerateVideos(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            yield break;
        }

        var url = GetFeedUrl(channelId);
        using var response = await httpReader.GetResponseAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            if (suppressHttpErrors)
            {
                yield break;
            }

            response.EnsureSuccessStatusCode();
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        var feed = XElement.Load(stream);

        var atom = feed.GetDefaultNamespace();
        var youtube = feed.GetNamespaceOfPrefix("yt") ?? XNamespace.None;
        var media = feed.GetNamespaceOfPrefix("media") ?? XNamespace.None;

        var channelName = feed.Element(atom.GetName("title"))?.Value;
        if (string.IsNullOrWhiteSpace(channelName))
        {
            yield break;
        }

        var channel = new YouTubeChannel(channelId, channelName);
        foreach (var entry in feed.Elements(atom.GetName("entry")))
        {
            var videoId = entry.Element(youtube.GetName("videoId"))?.Value;
            if (string.IsNullOrWhiteSpace(videoId))
            {
                continue;
            }

            var videoTitle = entry.Element(atom.GetName("title"))?.Value;
            if (string.IsNullOrWhiteSpace(videoTitle))
            {
                continue;
            }

            var videoDescription = entry.Element(media.GetName("group"))?.Element(media.GetName("description"))?.Value;
            if (string.IsNullOrWhiteSpace(videoDescription))
            {
                continue;
            }

            yield return new YouTubeVideo(channel, videoId, videoTitle, videoDescription);
        }
    }
}
