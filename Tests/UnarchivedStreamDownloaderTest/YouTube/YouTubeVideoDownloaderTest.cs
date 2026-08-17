namespace UnarchivedStreamDownloaderTest.YouTube;

using NSubstitute;
using Shouldly;
using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Core.YouTube;
using UnarchivedStreamDownloader.YouTube;

[TestClass]
public class YouTubeVideoDownloaderTest
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task DownloadAsync_LockCreated_ReturnsResult(bool result)
    {
        var videoId = "VideoID";
        var video = CreateYouTubeVideo(videoId);
        var downloader = CreateDownloader(out var lockFactory, out var lockObject, out var processRunner, true, result);

        (await downloader.DownloadAsync(video)).ShouldBe(result);

        lockFactory.Received(1).TryCreate($"UnarchivedStreamDownloader.{videoId}");
        processRunner.Received(1).Run(videoId);

        lockObject.ShouldNotBeNull();
        lockObject.Received(1).Dispose();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task DownloadAsync_LockFailed_ReturnsNull(bool result)
    {
        var videoId = "VideoID";
        var video = CreateYouTubeVideo(videoId);
        var downloader = CreateDownloader(out var lockFactory, out var lockObject, out var processRunner, false, result);

        (await downloader.DownloadAsync(video)).ShouldBeNull();

        lockFactory.Received(1).TryCreate($"UnarchivedStreamDownloader.{videoId}");
        processRunner.DidNotReceive().Run(Arg.Any<string>());

        lockObject.ShouldBeNull();
    }

    [TestMethod]
    public async Task DownloadAsync_ProcessRunnerThrows_ReturnsFalse()
    {
        var video = CreateYouTubeVideo("VideoID");
        var downloader = CreateDownloader(out _, out var lockObject, out var processRunner, true, true);
        processRunner.Run(Arg.Any<string>()).Returns(_ => throw new InvalidOperationException());

        (await downloader.DownloadAsync(video)).ShouldBe(false);

        lockObject.ShouldNotBeNull();
        lockObject.Received(1).Dispose();
    }

    private static YouTubeVideo CreateYouTubeVideo(string videoId)
    {
        var channel = new YouTubeChannel("ChannelID", "ChannelName");
        return new YouTubeVideo(channel, videoId, "VideoTitle", "VideoDescription");
    }

    private static YouTubeVideoDownloader CreateDownloader(
        out ILockFactory lockFactory,
        out IDisposable? lockObject,
        out IProcessRunner processRunner,
        bool lockResult,
        bool processResult)
    {
        lockFactory = Substitute.For<ILockFactory>();
        processRunner = Substitute.For<IProcessRunner>();

        lockObject = lockResult ? Substitute.For<IDisposable>() : null;
        lockFactory.TryCreate(Arg.Any<string>()).Returns(lockObject);
        processRunner.Run(Arg.Any<string>()).Returns(processResult);

        return new YouTubeVideoDownloader(Substitute.For<ILogger>(), lockFactory, processRunner);
    }
}
