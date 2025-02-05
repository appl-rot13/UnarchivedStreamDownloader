
namespace UnarchivedStreamDownloader.Utilities;

using System.Xml.Linq;

public class YouTubeDataRetriever
{
    public static string GetFeedUrl(string channelId)
    {
        return $"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}";
    }

    public static IEnumerable<((string Id, string Name) Channel, string Id, string Title)> EnumerateLatestVideos(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            yield break;
        }

        var url = GetFeedUrl(channelId);
        var feed = XElement.Load(url);

        var xmlNamespace = feed.GetDefaultNamespace();
        var youtubeNamespace = feed.GetNamespaceOfPrefix("yt") ?? XNamespace.None;

        var channelName = feed.Element(xmlNamespace.GetName("title"))?.Value;
        if (string.IsNullOrWhiteSpace(channelName))
        {
            yield break;
        }

        var channel = (channelId, channelName);
        foreach (var entry in feed.Elements(xmlNamespace.GetName("entry")))
        {
            var videoId = entry.Element(youtubeNamespace.GetName("videoId"))?.Value;
            if (string.IsNullOrWhiteSpace(videoId))
            {
                continue;
            }

            var videoTitle = entry.Element(xmlNamespace.GetName("title"))?.Value;
            if (string.IsNullOrWhiteSpace(videoTitle))
            {
                continue;
            }

            yield return (channel, videoId, videoTitle);
        }
    }
}
