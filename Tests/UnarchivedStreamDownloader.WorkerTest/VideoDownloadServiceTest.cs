namespace UnarchivedStreamDownloader.WorkerTest;

using System.Collections.Immutable;
using System.Text;
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

    [TestMethod]
    public async Task DownloadArchiveAsync_ArchiveFileExists_ReturnsTrue()
    {
        var videoId = "VideoID";
        var service = CreateService(out var startWaiter, out var downloader);
        downloader.ArchiveFileExists(Arg.Any<string>()).Returns(true);

        (await service.DownloadArchiveAsync(videoId)).ShouldBeTrue();

        downloader.Received(1).ArchiveFileExists(videoId);
        await startWaiter.DidNotReceive().WaitForStartAsync(Arg.Any<string>());
        await downloader.DidNotReceive().DownloadAsync(Arg.Any<string>());
    }

    [TestMethod]
    [DataRow(1, false)]
    [DataRow(0, true)]
    public async Task DownloadArchiveAsync_NoStartedOrNoAttempts_ReturnsFalse(int downloadAttempts, bool waitResult)
    {
        var videoId = "VideoID";
        var service = CreateService(out var startWaiter, out var downloader, downloadAttempts, waitResult);
        downloader.ArchiveFileExists(Arg.Any<string>()).Returns(false);

        (await service.DownloadArchiveAsync(videoId)).ShouldBeFalse();

        downloader.Received(1).ArchiveFileExists(videoId);
        await startWaiter.Received(1).WaitForStartAsync(videoId);
        await downloader.DidNotReceive().DownloadAsync(Arg.Any<string>());
    }

    [TestMethod]
    public async Task DownloadArchiveAsync_DownloadFails_ReturnsFalse()
    {
        var videoId = "VideoID";
        var service = CreateService(out var startWaiter, out var downloader, downloadResult: false);
        downloader.ArchiveFileExists(Arg.Any<string>()).Returns(false);

        (await service.DownloadArchiveAsync(videoId)).ShouldBeFalse();

        downloader.Received(1).ArchiveFileExists(videoId);
        await startWaiter.Received(1).WaitForStartAsync(videoId);
        await downloader.Received(1).DownloadAsync(Arg.Any<string>());
    }

    public static IEnumerable<DownloadArchiveAsyncTestCase> DownloadArchiveAsyncTestCases()
    {
        return
        [
            new DownloadArchiveAsyncTestCase([false, true              ], 3, 1,  true),
            new DownloadArchiveAsyncTestCase([false, false, true       ], 3, 2,  true),
            new DownloadArchiveAsyncTestCase([false, false, false, true], 3, 3,  true),
            new DownloadArchiveAsyncTestCase([false                    ], 3, 3, false),
            new DownloadArchiveAsyncTestCase([false                    ], 5, 5, false),
        ];
    }

    [TestMethod]
    [DynamicData(nameof(DownloadArchiveAsyncTestCases))]
    public async Task DownloadArchiveAsync_RetriesUntilArchiveFileExists(DownloadArchiveAsyncTestCase testCase)
    {
        var videoId = "VideoID";
        var service = CreateService(out var startWaiter, out var downloader, testCase.DownloadAttempts);
        downloader.ArchiveFileExists(Arg.Any<string>()).Returns(testCase.ArchiveFileExistsResults);

        (await service.DownloadArchiveAsync(videoId)).ShouldBe(testCase.ExpectedResult);

        downloader.Received(testCase.ExpectedDownloadCount + 1).ArchiveFileExists(videoId);
        await startWaiter.Received(1).WaitForStartAsync(videoId);
        await downloader.Received(testCase.ExpectedDownloadCount).DownloadAsync(Arg.Any<string>());
    }

    public static IEnumerable<DownloadWithRetryTestCase> DownloadWithRetryTestCases()
    {
        return
        [
            new DownloadWithRetryTestCase([true              ], 3, 1,  true),
            new DownloadWithRetryTestCase([false, true       ], 3, 2,  true),
            new DownloadWithRetryTestCase([false, false, true], 3, 3,  true),
            new DownloadWithRetryTestCase([false             ], 3, 3, false),
            new DownloadWithRetryTestCase([false             ], 5, 5, false),
            new DownloadWithRetryTestCase([true              ], 0, 0, false),
        ];
    }

    [TestMethod]
    [DynamicData(nameof(DownloadWithRetryTestCases))]
    public async Task DownloadWithRetryAsync_RetriesUntilDownloadSucceeds(DownloadWithRetryTestCase testCase)
    {
        var videoId = "VideoID";
        var service = CreateService(out var timeProvider, out var downloader, testCase.RetryAttempts, 3);

        using var semaphore = new SemaphoreSlim(0);
        downloader.DownloadAsync(Arg.Any<string>()).Returns(testCase.DownloadResults);
        downloader.When(t => t.DownloadAsync(Arg.Any<string>())).Do(_ => semaphore.Release());

        var task = service.DownloadWithRetryAsync(videoId);

        var cancellationToken = TestContext.CancellationToken;
        for (var i = 1; i < testCase.ExpectedDownloadCount; i++)
        {
            (await semaphore.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken)).ShouldBeTrue();

            await downloader.Received(i).DownloadAsync(videoId);
            timeProvider.Advance(TimeSpan.FromSeconds(2));
            await downloader.Received(i).DownloadAsync(videoId);
            timeProvider.Advance(TimeSpan.FromSeconds(1));
        }

        (await task).ShouldBe(testCase.ExpectedResult);
        await downloader.Received(testCase.ExpectedDownloadCount).DownloadAsync(videoId);
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
            Substitute.For<IYouTubeLiveStartWaiter>());
    }

    private static VideoDownloadService CreateService(
        out IYouTubeLiveStartWaiter startWaiter,
        out IVideoDownloader downloader,
        int downloadAttempts = 1,
        bool waitResult = true,
        bool downloadResult = true)
    {
        startWaiter = Substitute.For<IYouTubeLiveStartWaiter>();
        startWaiter.WaitForStartAsync(Arg.Any<string>()).Returns(waitResult);

        downloader = Substitute.For<IVideoDownloader>();
        downloader.DownloadAsync(Arg.Any<string>()).Returns(downloadResult);

        return new VideoDownloadService(
            Substitute.For<ILogger>(),
            new FakeTimeProvider(),
            CreateSettings(downloadAttempts, 1, 0),
            downloader,
            startWaiter);
    }

    public record DownloadArchiveAsyncTestCase(
        ImmutableArray<bool> ArchiveFileExistsResults,
        int DownloadAttempts,
        int ExpectedDownloadCount,
        bool ExpectedResult)
    {
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{nameof(ArchiveFileExistsResults)} = [{string.Join(", ", ArchiveFileExistsResults)}]");
            builder.Append(", ");
            builder.Append($"{nameof(DownloadAttempts)} = {DownloadAttempts}");
            builder.Append(", ");
            builder.Append($"{nameof(ExpectedDownloadCount)} = {ExpectedDownloadCount}");
            builder.Append(", ");
            builder.Append($"{nameof(ExpectedResult)} = {ExpectedResult}");

            return true;
        }
    }

    public record DownloadWithRetryTestCase(
        ImmutableArray<bool> DownloadResults,
        int RetryAttempts,
        int ExpectedDownloadCount,
        bool ExpectedResult)
    {
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{nameof(DownloadResults)} = [{string.Join(", ", DownloadResults)}]");
            builder.Append(", ");
            builder.Append($"{nameof(RetryAttempts)} = {RetryAttempts}");
            builder.Append(", ");
            builder.Append($"{nameof(ExpectedDownloadCount)} = {ExpectedDownloadCount}");
            builder.Append(", ");
            builder.Append($"{nameof(ExpectedResult)} = {ExpectedResult}");

            return true;
        }
    }
}
