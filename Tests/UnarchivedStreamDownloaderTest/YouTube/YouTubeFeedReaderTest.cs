namespace UnarchivedStreamDownloaderTest.YouTube;

using System.Net;
using System.Xml;
using System.Xml.Linq;
using NSubstitute;
using Shouldly;
using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Core.YouTube;
using UnarchivedStreamDownloader.YouTube;

[TestClass]
public class YouTubeFeedReaderTest
{
    [TestMethod]
    [DataRow("ChannelID", "https://www.youtube.com/feeds/videos.xml?channel_id=ChannelID")]
    public void GetFeedUrl_ReturnsUrl(string channelId, string expected)
    {
        YouTubeFeedReader.GetFeedUrl(channelId).ShouldBe(expected);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    public void GetFeedUrl_InvalidChannelId_ThrowsArgumentException(string channelId)
    {
        Should.Throw<ArgumentException>(() => YouTubeFeedReader.GetFeedUrl(channelId));
    }

    [TestMethod]
    [DataRow(null)]
    public void GetFeedUrl_NullChannelId_ThrowsArgumentNullException(string channelId)
    {
        Should.Throw<ArgumentNullException>(() => YouTubeFeedReader.GetFeedUrl(channelId));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task EnumerateVideos_InvalidChannelId_ReturnsEmpty(string channelId)
    {
        var feedReader = CreateFeedReader();
        (await feedReader.EnumerateVideos(channelId, false).ToListAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EnumerateVideos_SuppressHttpErrors_ReturnsEmpty()
    {
        var feedReader = CreateFeedReader(CreateNotFoundResponse());
        (await feedReader.EnumerateVideos("ChannelID", true).ToListAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EnumerateVideos_DoNotSuppressHttpErrors_ThrowsHttpRequestException()
    {
        var feedReader = CreateFeedReader(CreateNotFoundResponse());
        Should.Throw<HttpRequestException>(async () => await feedReader.EnumerateVideos("ChannelID", false).ToListAsync());
    }

    [TestMethod]
    public async Task EnumerateVideos_InvalidXml_ThrowsXmlException()
    {
        var feedReader = CreateFeedReader(CreateInvalidResponse());
        Should.Throw<XmlException>(async () => await feedReader.EnumerateVideos("ChannelID", false).ToListAsync());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task EnumerateVideos_InvalidChannelName_ReturnsEmpty(string? channelName)
    {
        var feed = CreateFeed(channelName, [("VideoID", "VideoTitle", "VideoDescription")]);
        var feedReader = CreateFeedReader(CreateSuccessResponse(feed));

        (await feedReader.EnumerateVideos("ChannelID", false).ToListAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EnumerateVideos_NoEntry_ReturnsEmpty()
    {
        var feed = CreateFeed("ChannelName", []);
        var feedReader = CreateFeedReader(CreateSuccessResponse(feed));

        (await feedReader.EnumerateVideos("ChannelID", false).ToListAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow("", "VideoTitle-2", "VideoDescription-2")]
    [DataRow(" ", "VideoTitle-2", "VideoDescription-2")]
    [DataRow(null, "VideoTitle-2", "VideoDescription-2")]
    [DataRow("VideoID-2", "", "VideoDescription-2")]
    [DataRow("VideoID-2", " ", "VideoDescription-2")]
    [DataRow("VideoID-2", null, "VideoDescription-2")]
    [DataRow("VideoID-2", "VideoTitle-2", "")]
    [DataRow("VideoID-2", "VideoTitle-2", " ")]
    [DataRow("VideoID-2", "VideoTitle-2", null)]
    public async Task EnumerateVideos_InvalidEntry_SkipsEntry(string? videoId, string? videoTitle, string? videoDescription)
    {
        var channel = new YouTubeChannel("ChannelID", "ChannelName");
        var feed = CreateFeed(
            channel.Name,
            [
                ("VideoID-1", "VideoTitle-1", "VideoDescription-1"),
                (videoId, videoTitle, videoDescription),
                ("VideoID-3", "VideoTitle-3", "VideoDescription-3"),
            ]);
        var feedReader = CreateFeedReader(CreateSuccessResponse(feed));

        (await feedReader.EnumerateVideos(channel.Id, false).ToListAsync()).ShouldBe([
            new YouTubeVideo(channel, "VideoID-1", "VideoTitle-1", "VideoDescription-1"),
            new YouTubeVideo(channel, "VideoID-3", "VideoTitle-3", "VideoDescription-3"),
        ]);
    }

    [TestMethod]
    public async Task EnumerateVideos_ReturnsVideos()
    {
        var channel = new YouTubeChannel("ChannelID", "ChannelName");
        var feed = CreateFeed(
            channel.Name,
            [
                ("VideoID-1", "VideoTitle-1", "VideoDescription-1"),
                ("VideoID-2", "VideoTitle-2", "VideoDescription-2"),
                ("VideoID-3", "VideoTitle-3", "VideoDescription-3"),
            ]);
        var feedReader = CreateFeedReader(CreateSuccessResponse(feed));

        (await feedReader.EnumerateVideos(channel.Id, false).ToListAsync()).ShouldBe([
            new YouTubeVideo(channel, "VideoID-1", "VideoTitle-1", "VideoDescription-1"),
            new YouTubeVideo(channel, "VideoID-2", "VideoTitle-2", "VideoDescription-2"),
            new YouTubeVideo(channel, "VideoID-3", "VideoTitle-3", "VideoDescription-3"),
        ]);
    }

    private static XElement CreateFeed(string? channelName, IEnumerable<(string? videoId, string? title, string? description)> entries)
    {
        XNamespace atom = "http://www.w3.org/2005/Atom";
        XNamespace yt = "http://www.youtube.com/xml/schemas/2015";
        XNamespace media = "http://search.yahoo.com/mrss/";

        return new XElement(
            atom + "feed",
            new XAttribute(XNamespace.Xmlns + "yt", yt),
            new XAttribute(XNamespace.Xmlns + "media", media),
            channelName == null ? null : new XElement(atom + "title", channelName),
            entries.Select(
                entry => new XElement(
                    atom + "entry",
                    entry.videoId == null ? null : new XElement(yt + "videoId", entry.videoId),
                    entry.title == null ? null : new XElement(atom + "title", entry.title),
                    new XElement(
                        media + "group",
                        entry.description == null ? null : new XElement(media + "description", entry.description)
                    )
                )
            )
        );
    }

    private static HttpResponseMessage CreateNotFoundResponse()
    {
        var content = "<!DOCTYPE html><html lang=en>";
        return CreateHttpResponse(HttpStatusCode.NotFound, content);
    }

    private static HttpResponseMessage CreateInvalidResponse()
    {
        var content = "<!DOCTYPE html><html lang=en>";
        return CreateHttpResponse(HttpStatusCode.OK, content);
    }

    private static HttpResponseMessage CreateSuccessResponse(XElement feed)
    {
        var content = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>{Environment.NewLine}{feed}";
        return CreateHttpResponse(HttpStatusCode.OK, content);
    }

    private static HttpResponseMessage CreateHttpResponse(HttpStatusCode statusCode, string content)
    {
        return new HttpResponseMessage(statusCode) { Content = new StringContent(content) };
    }

    private static YouTubeFeedReader CreateFeedReader(HttpResponseMessage? response = null)
    {
        var httpReader = Substitute.For<IHttpReader>();
        if (response != null)
        {
            httpReader.GetResponseAsync(Arg.Any<string>()).Returns(Task.FromResult(response));
        }

        return new YouTubeFeedReader(httpReader);
    }
}
