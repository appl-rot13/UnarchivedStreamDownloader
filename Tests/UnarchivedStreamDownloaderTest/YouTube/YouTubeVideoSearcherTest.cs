
namespace UnarchivedStreamDownloaderTest.YouTube;

using NSubstitute;

using Shouldly;

using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.YouTube;
using UnarchivedStreamDownloader.YouTube;

[TestClass]
public class YouTubeVideoSearcherTest
{
    [TestMethod]
    public async Task EnumerateMatchingVideos_NoVideos_ReturnsEmpty()
    {
        var searcher = CreateSearcher([], [string.Empty]);
        (await searcher.EnumerateMatchingVideos("ChannelID", false).ToListAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EnumerateMatchingVideos_NoKeywords_ReturnsEmpty()
    {
        var (channel, videos) = CreateYouTubeData();
        var searcher = CreateSearcher(videos, []);

        (await searcher.EnumerateMatchingVideos(channel.Id, false).ToListAsync()).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task EnumerateMatchingVideos_KeywordsContainEmptyString_ReturnsAllVideos()
    {
        var (channel, videos) = CreateYouTubeData();
        var searcher = CreateSearcher(videos, [string.Empty]);

        (await searcher.EnumerateMatchingVideos(channel.Id, false).ToListAsync()).ShouldBe(videos);
    }

    [TestMethod]
    public async Task EnumerateMatchingVideos_VideoIdToIgnored_SkipsVideo()
    {
        var (channel, videos) = CreateYouTubeData();
        var searcher = CreateSearcher(videos, ["Video"], ["VideoID-2"]);

        (await searcher.EnumerateMatchingVideos(channel.Id, false).ToListAsync()).ShouldBe(
        [
            new YouTubeVideo(channel, "VideoID-1", "VideoTitle-1", "VideoDescription-1"),
            new YouTubeVideo(channel, "VideoID-3", "VideoTitle-3", "VideoDescription-3"),
        ]);
    }

    [TestMethod]
    public async Task EnumerateMatchingVideos_KeywordMatchesAll_ReturnsAllVideos()
    {
        var (channel, videos) = CreateYouTubeData();
        var searcher = CreateSearcher(videos, ["Video"]);

        (await searcher.EnumerateMatchingVideos(channel.Id, false).ToListAsync()).ShouldBe(
        [
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
    public async Task EnumerateMatchingVideos_KeywordMatches_ReturnsMatchingVideos(string keyword)
    {
        var (channel, videos) = CreateYouTubeData();
        var searcher = CreateSearcher(videos, [keyword]);

        (await searcher.EnumerateMatchingVideos(channel.Id, false).ToListAsync()).ShouldBe(
        [
            new YouTubeVideo(channel, "VideoID-2", "VideoTitle-2", "VideoDescription-2"),
        ]);
    }

    [TestMethod]
    public async Task EnumerateMatchingVideos_MultipleKeywordsMatch_ReturnsMatchingVideos()
    {
        var (channel, videos) = CreateYouTubeData();
        var searcher = CreateSearcher(videos, ["Title-1", "Description-2"]);

        (await searcher.EnumerateMatchingVideos(channel.Id, false).ToListAsync()).ShouldBe(
        [
            new YouTubeVideo(channel, "VideoID-1", "VideoTitle-1", "VideoDescription-1"),
            new YouTubeVideo(channel, "VideoID-2", "VideoTitle-2", "VideoDescription-2"),
        ]);
    }

    [TestMethod]
    [DataRow("ChannelID-1", false)]
    [DataRow("ChannelID-2", true)]
    public async Task EnumerateMatchingVideos_PassesArgumentsToFeedReader(string channelId, bool suppressHttpErrors)
    {
        var searcher = CreateSearcher(out var reader, [], []);
        await searcher.EnumerateMatchingVideos(channelId, suppressHttpErrors).ToListAsync();

        reader.Received(1).EnumerateLatestVideos(channelId, suppressHttpErrors);
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

    private static YouTubeVideoSearcher CreateSearcher(
        IEnumerable<YouTubeVideo> videos,
        IReadOnlyCollection<string> keywords,
        IReadOnlyCollection<string>? ignoreVideoIds = null)
    {
        return CreateSearcher(out _, videos, keywords, ignoreVideoIds);
    }

    private static YouTubeVideoSearcher CreateSearcher(
        out IYouTubeFeedReader reader,
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

        reader = Substitute.For<IYouTubeFeedReader>();
        reader.EnumerateLatestVideos(Arg.Any<string>(), Arg.Any<bool>()).Returns(videos.ToAsyncEnumerable());

        return new YouTubeVideoSearcher(settings, reader);
    }
}
