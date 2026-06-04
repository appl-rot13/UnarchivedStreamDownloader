
namespace UnarchivedStreamDownloader.YouTube;

using System.Xml.Linq;

using UnarchivedStreamDownloader.Core.Utilities;

public class YouTubeDataRetriever(IHttpReader httpReader)
{
    public static string GetFeedUrl(string channelId)
    {
        return $"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}";
    }

    public async IAsyncEnumerable<YouTubeVideo> EnumerateLatestVideos(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            yield break;
        }

        var url = GetFeedUrl(channelId);
        var feed = XElement.Parse(await httpReader.GetResponseAsync(url));

        var xmlNamespace = feed.GetDefaultNamespace();
        var youtubeNamespace = feed.GetNamespaceOfPrefix("yt") ?? XNamespace.None;
        var mediaNamespace = feed.GetNamespaceOfPrefix("media") ?? XNamespace.None;

        var channelName = feed.Element(xmlNamespace.GetName("title"))?.Value;
        if (string.IsNullOrWhiteSpace(channelName))
        {
            yield break;
        }

        var channel = new YouTubeChannel(channelId, channelName);
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

            var videoDescription = entry.Element(mediaNamespace.GetName("group"))?.Element(mediaNamespace.GetName("description"))?.Value;
            if (string.IsNullOrWhiteSpace(videoDescription))
            {
                continue;
            }

            yield return new YouTubeVideo(channel, videoId, videoTitle, videoDescription);
        }
    }

    public async IAsyncEnumerable<YouTubeVideo> EnumerateLatestVideos(string channelId, bool suppressHttpErrors)
    {
        await using var enumerator = this.EnumerateLatestVideos(channelId).GetAsyncEnumerator();
        while (true)
        {
            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    yield break;
                }
            }
            catch (HttpRequestException)
            {
                if (suppressHttpErrors)
                {
                    yield break;
                }

                throw;
            }

            yield return enumerator.Current;
        }
    }
}
