namespace UnarchivedStreamDownloaderTest.YouTube;

using NSubstitute;
using Shouldly;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.YouTube;
using UnarchivedStreamDownloader.YouTube;

[TestClass]
public class YouTubeVideoFilterTest
{
    [TestMethod]
    public async Task EnumerateVideos_NoVideos_ReturnsEmpty()
    {
        var filter = CreateFilter([], [string.Empty]);
        (await filter.EnumerateVideos("ChannelID", false).ToListAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EnumerateVideos_NoKeywords_ReturnsEmpty()
    {
        var (channel, videos) = CreateYouTubeData();
        var filter = CreateFilter(videos, []);

        (await filter.EnumerateVideos(channel.Id, false).ToListAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EnumerateVideos_KeywordsContainEmptyString_ReturnsAllVideos()
    {
        var (channel, videos) = CreateYouTubeData();
        var filter = CreateFilter(videos, [string.Empty]);

        (await filter.EnumerateVideos(channel.Id, false).ToListAsync()).ShouldBe(videos);
    }

    [TestMethod]
    public async Task EnumerateVideos_VideoIdToIgnored_SkipsVideo()
    {
        var (channel, videos) = CreateYouTubeData();
        var filter = CreateFilter(videos, ["Video"], ["VideoID-2"]);

        (await filter.EnumerateVideos(channel.Id, false).ToListAsync()).ShouldBe([
            new YouTubeVideo(channel, "VideoID-1", "VideoTitle-1", "VideoDescription-1"),
            new YouTubeVideo(channel, "VideoID-3", "VideoTitle-3", "VideoDescription-3"),
        ]);
    }

    [TestMethod]
    public async Task EnumerateVideos_KeywordMatchesAll_ReturnsAllVideos()
    {
        var (channel, videos) = CreateYouTubeData();
        var filter = CreateFilter(videos, ["Video"]);

        (await filter.EnumerateVideos(channel.Id, false).ToListAsync()).ShouldBe([
            new YouTubeVideo(channel, "VideoID-1", "VideoTitle-1", "VideoDescription-1"),
            new YouTubeVideo(channel, "VideoID-2", "VideoTitle-2", "VideoDescription-2"),
            new YouTubeVideo(channel, "VideoID-3", "VideoTitle-3", "VideoDescription-3"),
        ]);
    }

    [TestMethod]
    [DataRow("Title-2")]
    [DataRow("TITLE-2")]
    [DataRow("Description-2")]
    [DataRow("description-2")]
    public async Task EnumerateVideos_KeywordMatches_ReturnsMatchingVideos(string keyword)
    {
        var (channel, videos) = CreateYouTubeData();
        var filter = CreateFilter(videos, [keyword]);

        (await filter.EnumerateVideos(channel.Id, false).ToListAsync()).ShouldBe([
            new YouTubeVideo(channel, "VideoID-2", "VideoTitle-2", "VideoDescription-2"),
        ]);
    }

    [TestMethod]
    public async Task EnumerateVideos_MultipleKeywordsMatch_ReturnsMatchingVideos()
    {
        var (channel, videos) = CreateYouTubeData();
        var filter = CreateFilter(videos, ["Title-1", "Description-2"]);

        (await filter.EnumerateVideos(channel.Id, false).ToListAsync()).ShouldBe([
            new YouTubeVideo(channel, "VideoID-1", "VideoTitle-1", "VideoDescription-1"),
            new YouTubeVideo(channel, "VideoID-2", "VideoTitle-2", "VideoDescription-2"),
        ]);
    }

    [TestMethod]
    [DataRow("ChannelID-1", false)]
    [DataRow("ChannelID-2", true)]
    public async Task EnumerateVideos_PassesArguments(string channelId, bool suppressHttpErrors)
    {
        var filter = CreateFilter(out var reader, [], []);
        await filter.EnumerateVideos(channelId, suppressHttpErrors).ToListAsync();

        reader.Received(1).EnumerateVideos(channelId, suppressHttpErrors);
    }

    private static (YouTubeChannel, IReadOnlyList<YouTubeVideo>) CreateYouTubeData()
    {
        var channel = new YouTubeChannel("ChannelID", "ChannelName");
        IReadOnlyList<YouTubeVideo> videos =
        [
            new YouTubeVideo(channel, "VideoID-1", "VideoTitle-1", "VideoDescription-1"),
            new YouTubeVideo(channel, "VideoID-2", "VideoTitle-2", "VideoDescription-2"),
            new YouTubeVideo(channel, "VideoID-3", "VideoTitle-3", "VideoDescription-3"),
        ];

        return (channel, videos);
    }

    private static YouTubeVideoFilter CreateFilter(
        IEnumerable<YouTubeVideo> videos,
        IReadOnlyCollection<string> keywords,
        IReadOnlyCollection<string>? ignoreVideoIds = null)
    {
        return CreateFilter(out _, videos, keywords, ignoreVideoIds);
    }

    private static YouTubeVideoFilter CreateFilter(
        out IYouTubeVideoSource source,
        IEnumerable<YouTubeVideo> videos,
        IReadOnlyCollection<string> keywords,
        IReadOnlyCollection<string>? ignoreVideoIds = null)
    {
        var settings = new SearchSettings
        {
            ChannelIDs = [],
            IgnoreVideoIDs = ignoreVideoIds ?? [],
            Keywords = keywords,
        };

        source = Substitute.For<IYouTubeVideoSource>();
        source.EnumerateVideos(Arg.Any<string>(), Arg.Any<bool>()).Returns(videos.ToAsyncEnumerable());

        return new YouTubeVideoFilter(settings, source);
    }
}
