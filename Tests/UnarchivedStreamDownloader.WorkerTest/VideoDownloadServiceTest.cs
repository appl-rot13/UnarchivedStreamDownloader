namespace UnarchivedStreamDownloader.WorkerTest;

using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Worker;

[TestClass]
public class VideoDownloadServiceTest
{
    public TestContext TestContext { get; set; }

    private CancellationToken CancellationToken => this.TestContext.CancellationToken;

    [TestMethod]
    public async Task DownloadArchiveAsync_ArchiveFileExists_ReturnsTrue()
    {
        var videoId = "VideoID";
        var service = CreateService(out var videoWaiter, out var downloader);
        downloader.ArchiveFileExists(Arg.Any<string>()).Returns(true);

        (await service.DownloadArchiveAsync(videoId)).ShouldBeTrue();

        downloader.Received(1).ArchiveFileExists(videoId);
        await videoWaiter.DidNotReceive().WaitAsync(Arg.Any<string>());
        await downloader.DidNotReceive().DownloadAsync(Arg.Any<string>());
    }

    [TestMethod]
    [DataRow(1, false)]
    [DataRow(0, true)]
    public async Task DownloadArchiveAsync_NoStartedOrNoAttempts_ReturnsFalse(int downloadAttempts, bool waitResult)
    {
        var videoId = "VideoID";
        var service = CreateService(out var videoWaiter, out var downloader, downloadAttempts, waitResult);
        downloader.ArchiveFileExists(Arg.Any<string>()).Returns(false);

        (await service.DownloadArchiveAsync(videoId)).ShouldBeFalse();

        downloader.Received(1).ArchiveFileExists(videoId);
        await videoWaiter.Received(1).WaitAsync(videoId);
        await downloader.DidNotReceive().DownloadAsync(Arg.Any<string>());
    }

    [TestMethod]
    public async Task DownloadArchiveAsync_DownloadFails_ReturnsFalse()
    {
        var videoId = "VideoID";
        var service = CreateService(out var videoWaiter, out var downloader, downloadResult: false);
        downloader.ArchiveFileExists(Arg.Any<string>()).Returns(false);

        (await service.DownloadArchiveAsync(videoId)).ShouldBeFalse();

        downloader.Received(1).ArchiveFileExists(videoId);
        await videoWaiter.Received(1).WaitAsync(videoId);
        await downloader.Received(1).DownloadAsync(Arg.Any<string>());
    }

    [TestMethod]
    [DataRow(new bool[] { false, true               }, 3, 1,  true)]
    [DataRow(new bool[] { false, false, true        }, 3, 2,  true)]
    [DataRow(new bool[] { false, false, false, true }, 3, 3,  true)]
    [DataRow(new bool[] { false                     }, 3, 3, false)]
    [DataRow(new bool[] { false                     }, 5, 5, false)]
    public async Task DownloadArchiveAsync_RetriesUntilArchiveFileExists(
        IReadOnlyList<bool> archiveFileExistsResults,
        int downloadAttempts,
        int expectedDownloadCount,
        bool expectedResult)
    {
        var videoId = "VideoID";
        var service = CreateService(out var videoWaiter, out var downloader, downloadAttempts);
        downloader.ArchiveFileExists(Arg.Any<string>()).Returns(archiveFileExistsResults);

        (await service.DownloadArchiveAsync(videoId)).ShouldBe(expectedResult);

        downloader.Received(expectedDownloadCount + 1).ArchiveFileExists(videoId);
        await videoWaiter.Received(1).WaitAsync(videoId);
        await downloader.Received(expectedDownloadCount).DownloadAsync(Arg.Any<string>());
    }

    [TestMethod]
    [DataRow(new bool[] { true               }, 3, 1,  true)]
    [DataRow(new bool[] { false, true        }, 3, 2,  true)]
    [DataRow(new bool[] { false, false, true }, 3, 3,  true)]
    [DataRow(new bool[] { false              }, 3, 3, false)]
    [DataRow(new bool[] { false              }, 5, 5, false)]
    [DataRow(new bool[] { true               }, 0, 0, false)]
    public async Task DownloadWithRetryAsync_RetriesUntilDownloadSucceeds(
        IReadOnlyList<bool> downloadResults,
        int retryAttempts,
        int expectedDownloadCount,
        bool expectedResult)
    {
        var videoId = "VideoID";
        var service = CreateService(out var timeProvider, out var downloader, retryAttempts, 3);

        using var semaphore = new SemaphoreSlim(0);
        downloader.DownloadAsync(Arg.Any<string>()).Returns(downloadResults);
        downloader.When(t => t.DownloadAsync(Arg.Any<string>())).Do(_ => semaphore.Release());

        var task = service.DownloadWithRetryAsync(videoId);

        for (var i = 1; i < expectedDownloadCount; i++)
        {
            (await semaphore.WaitAsync(TimeSpan.FromSeconds(1), this.CancellationToken)).ShouldBeTrue();

            await downloader.Received(i).DownloadAsync(videoId);
            timeProvider.Advance(TimeSpan.FromSeconds(2));
            await downloader.Received(i).DownloadAsync(videoId);
            timeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        (await task).ShouldBe(expectedResult);
        await downloader.Received(expectedDownloadCount).DownloadAsync(videoId);
    }

    private static BehaviorSettings CreateSettings(
        int downloadAttempts,
        int errorRetryAttempts,
        int errorRetryIntervalSeconds)
    {
        return new BehaviorSettings
        {
            // DownloadArchiveAsyncで使用
            DownloadAttempts = downloadAttempts,

            // DownloadWithRetryAsyncで使用
            ErrorRetryAttempts = errorRetryAttempts,
            ErrorRetryIntervalSeconds = errorRetryIntervalSeconds,

            // 以下は未使用
            StartCheckBufferSeconds = 0,
            StartCheckIntervalSeconds = 0,
        };
    }

    private static VideoDownloadService CreateService(
        out FakeTimeProvider timeProvider,
        out IVideoDownloader downloader,
        int errorRetryAttempts,
        int errorRetryIntervalSeconds)
    {
        return new VideoDownloadService(
            Substitute.For<ILogger>(),
            timeProvider = new FakeTimeProvider(),
            CreateSettings(0, errorRetryAttempts, errorRetryIntervalSeconds),
            downloader = Substitute.For<IVideoDownloader>(),
            Substitute.For<IVideoWaiter>());
    }

    private static VideoDownloadService CreateService(
        out IVideoWaiter videoWaiter,
        out IVideoDownloader downloader,
        int downloadAttempts = 1,
        bool waitResult = true,
        bool downloadResult = true)
    {
        videoWaiter = Substitute.For<IVideoWaiter>();
        videoWaiter.WaitAsync(Arg.Any<string>()).Returns(waitResult);

        downloader = Substitute.For<IVideoDownloader>();
        downloader.DownloadAsync(Arg.Any<string>()).Returns(downloadResult);

        return new VideoDownloadService(
            Substitute.For<ILogger>(),
            new FakeTimeProvider(),
            CreateSettings(downloadAttempts, 1, 0),
            downloader,
            videoWaiter);
    }
}
