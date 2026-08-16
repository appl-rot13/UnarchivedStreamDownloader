
namespace UnarchivedStreamDownloaderTest.YouTube;

using NSubstitute;

using Shouldly;

using System.Collections.Immutable;
using System.Text;

using UnarchivedStreamDownloader.Core.YouTube;
using UnarchivedStreamDownloader.YouTube;

[TestClass]
public class YouTubeVideoDownloadServiceTest
{
    public static IEnumerable<DownloadResultsTestCase> DownloadResultsTestCases() =>
    [
        new DownloadResultsTestCase([ true,  true,  true], [ true,  true,  true]),
        new DownloadResultsTestCase([ true, false,  true], [ true, false,  true]),
        new DownloadResultsTestCase([false, false, false], [false, false, false]),
        new DownloadResultsTestCase([ true,  null,  true], [ true,  true]),
        new DownloadResultsTestCase([false,  null, false], [false, false]),
        new DownloadResultsTestCase([ true,  null, false], [ true, false]),
        new DownloadResultsTestCase([ null,  null,  null], []),
    ];

    [TestMethod]
    public async Task DownloadAllAsync_NoVideos_ReturnsEmpty()
    {
        var service = CreateService([]);
        (await service.DownloadAllAsync(["ChannelID"], false)).ShouldBeEmpty();
    }

    [TestMethod]
    [DynamicData(nameof(DownloadResultsTestCases))]
    public async Task DownloadAllAsync_ReturnsDownloadResults(DownloadResultsTestCase testCase)
    {
        var videos = CreateYouTubeVideos();
        var channelIds = videos.Select(video => video.Channel.Id);
        var service = CreateService(videos.Zip(testCase.Results));

        (await service.DownloadAllAsync(channelIds, false)).ShouldBe(testCase.Expected, ignoreOrder: true);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task DownloadAllAsync_PassesArguments(bool suppressHttpErrors)
    {
        var videos = CreateYouTubeVideos();
        var channelIds = new[] { "ChannelID-1", " ChannelID-1 ", " ChannelID-2 " };
        var service = CreateService(out var searcher, out var downloader, videos.Select(video => (video, (bool?)true)));

        await service.DownloadAllAsync(channelIds, suppressHttpErrors);

        searcher.Received(2).EnumerateMatchingVideos(Arg.Any<string>(), Arg.Any<bool>());
        searcher.Received(1).EnumerateMatchingVideos("ChannelID-1", suppressHttpErrors);
        searcher.Received(1).EnumerateMatchingVideos("ChannelID-2", suppressHttpErrors);

        await downloader.Received(videos.Count).DownloadAsync(Arg.Any<YouTubeVideo>());
        foreach (var video in videos)
        {
            await downloader.Received(1).DownloadAsync(video);
        }
    }

    private static IReadOnlyList<YouTubeVideo> CreateYouTubeVideos()
    {
        return [
            new YouTubeVideo(new YouTubeChannel("ChannelID-1", "ChannelName-1"), "VideoID-1", "VideoTitle-1", "VideoDescription-1"),
            new YouTubeVideo(new YouTubeChannel("ChannelID-2", "ChannelName-2"), "VideoID-2", "VideoTitle-2", "VideoDescription-2"),
            new YouTubeVideo(new YouTubeChannel("ChannelID-2", "ChannelName-2"), "VideoID-3", "VideoTitle-3", "VideoDescription-3"),
        ];
    }

    private static YouTubeVideoDownloadService CreateService(
        IEnumerable<(YouTubeVideo Video, bool? Result)> downloadResults)
    {
        return CreateService(out _, out _, downloadResults);
    }

    private static YouTubeVideoDownloadService CreateService(
        out IYouTubeVideoSearcher searcher,
        out IYouTubeVideoDownloader downloader,
        IEnumerable<(YouTubeVideo Video, bool? Result)> downloadResults)
    {
        searcher = Substitute.For<IYouTubeVideoSearcher>();
        downloader = Substitute.For<IYouTubeVideoDownloader>();

        searcher.EnumerateMatchingVideos(Arg.Any<string>(), Arg.Any<bool>()).Returns(Enumerable.Empty<YouTubeVideo>().ToAsyncEnumerable());
        foreach (var group in downloadResults.GroupBy(t => t.Video.Channel.Id))
        {
            var channelId = group.Key;
            var videos = group.Select(t => t.Video);

            searcher.EnumerateMatchingVideos(channelId, Arg.Any<bool>()).Returns(videos.ToAsyncEnumerable());
            foreach (var (video, result) in group)
            {
                downloader.DownloadAsync(video).Returns(Task.FromResult(result));
            }
        }

        return new YouTubeVideoDownloadService(searcher, downloader);
    }

    public record DownloadResultsTestCase(ImmutableArray<bool?> Results, ImmutableArray<bool> Expected)
    {
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{nameof(Results)} = [{string.Join(", ", Results.Select(b => b?.ToString() ?? "null"))}]");
            builder.Append(", ");
            builder.Append($"{nameof(Expected)} = [{string.Join(", ", Expected)}]");

            return true;
        }
    }
}
