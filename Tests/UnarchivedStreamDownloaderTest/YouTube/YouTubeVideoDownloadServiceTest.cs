namespace UnarchivedStreamDownloaderTest.YouTube;

using System.Collections.Immutable;
using System.Text;
using NSubstitute;
using Shouldly;
using UnarchivedStreamDownloader.YouTube;

[TestClass]
public class YouTubeVideoDownloadServiceTest
{
    public static IEnumerable<DownloadAllAsyncTestCase> DownloadAllAsyncTestCases()
    {
        return
        [
            new DownloadAllAsyncTestCase([ true,  true,  true], [ true,  true,  true]),
            new DownloadAllAsyncTestCase([ true, false,  true], [ true, false,  true]),
            new DownloadAllAsyncTestCase([false, false, false], [false, false, false]),
            new DownloadAllAsyncTestCase([ true,  null,  true], [ true,  true]),
            new DownloadAllAsyncTestCase([false,  null, false], [false, false]),
            new DownloadAllAsyncTestCase([ true,  null, false], [ true, false]),
            new DownloadAllAsyncTestCase([ null,  null,  null], []),
        ];
    }

    [TestMethod]
    public async Task DownloadAllAsync_NoChannels_ReturnsEmpty()
    {
        var service = CreateService([]);
        (await service.DownloadAllAsync([])).ShouldBeEmpty();
    }

    [TestMethod]
    public async Task DownloadAllAsync_NoVideos_ReturnsEmpty()
    {
        var service = CreateService([]);
        (await service.DownloadAllAsync(["ChannelID"])).ShouldBeEmpty();
    }

    [TestMethod]
    [DynamicData(nameof(DownloadAllAsyncTestCases))]
    public async Task DownloadAllAsync_ReturnsResults(DownloadAllAsyncTestCase testCase)
    {
        var videos = CreateYouTubeVideos();
        var channelIds = videos.Select(video => video.Channel.Id);
        var service = CreateService(videos.Zip(testCase.DownloadResults));

        (await service.DownloadAllAsync(channelIds)).ShouldBe(testCase.ExpectedResults, ignoreOrder: true);
    }

    [TestMethod]
    public async Task DownloadAllAsync_PassesArguments()
    {
        var videos = CreateYouTubeVideos();
        var channelIds = new[] { "ChannelID-1", " ChannelID-1 ", " ChannelID-2 " };
        var service = CreateService(out var searcher, out var downloader, videos.Select(video => (video, (bool?)true)));

        await service.DownloadAllAsync(channelIds);

        searcher.Received(2).EnumerateVideos(Arg.Any<string>());
        searcher.Received(1).EnumerateVideos("ChannelID-1");
        searcher.Received(1).EnumerateVideos("ChannelID-2");

        await downloader.Received(videos.Count).DownloadAsync(Arg.Any<YouTubeVideo>());
        foreach (var video in videos)
        {
            await downloader.Received(1).DownloadAsync(video);
        }
    }

    private static IReadOnlyList<YouTubeVideo> CreateYouTubeVideos()
    {
        return
        [
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
        out IYouTubeVideoSource source,
        out IYouTubeVideoDownloader downloader,
        IEnumerable<(YouTubeVideo Video, bool? Result)> downloadResults)
    {
        source = Substitute.For<IYouTubeVideoSource>();
        downloader = Substitute.For<IYouTubeVideoDownloader>();

        source.EnumerateVideos(Arg.Any<string>()).Returns(Enumerable.Empty<YouTubeVideo>().ToAsyncEnumerable());
        foreach (var group in downloadResults.GroupBy(t => t.Video.Channel.Id))
        {
            var channelId = group.Key;
            var videos = group.Select(t => t.Video);

            source.EnumerateVideos(channelId).Returns(videos.ToAsyncEnumerable());
            foreach (var (video, result) in group)
            {
                downloader.DownloadAsync(video).Returns(result);
            }
        }

        return new YouTubeVideoDownloadService(source, downloader);
    }

    public record DownloadAllAsyncTestCase(ImmutableArray<bool?> DownloadResults, ImmutableArray<bool> ExpectedResults)
    {
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{nameof(DownloadResults)} = [{string.Join(", ", DownloadResults.Select(b => b?.ToString() ?? "null"))}]");
            builder.Append(", ");
            builder.Append($"{nameof(ExpectedResults)} = [{string.Join(", ", ExpectedResults)}]");

            return true;
        }
    }
}
