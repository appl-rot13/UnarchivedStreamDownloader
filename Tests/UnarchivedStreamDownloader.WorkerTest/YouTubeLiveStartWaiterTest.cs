namespace UnarchivedStreamDownloader.WorkerTest;

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shouldly;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Worker;

[TestClass]
public class YouTubeLiveStartWaiterTest
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task WaitForStartAsync_NullJson_ThrowsInvalidOperationException()
    {
        var waiter = CreateWaiter("null");
        Should.Throw<InvalidOperationException>(async () => await waiter.WaitForStartAsync("VideoID"));
    }

    [TestMethod]
    [DataRow(@"{}")]
    [DataRow(@"{""live_status"":""is_live""}")]
    [DataRow(@"{""release_timestamp"":0}")]
    public async Task WaitForStartAsync_MissingRequiredJson_ThrowsInvalidOperationException(string videoDetails)
    {
        var waiter = CreateWaiter(videoDetails);
        Should.Throw<InvalidOperationException>(async () => await waiter.WaitForStartAsync("VideoID"));
    }

    [TestMethod]
    [DataRow(null, null, false)]
    [DataRow("is_live", null, true)]
    public async Task WaitForStartAsync_EarlyReturnsResult(string? status, DateTimeOffset? timestamp, bool expected)
    {
        var videoId = "VideoID";
        var videoDetails = CreateVideoDetails(status, timestamp);
        var waiter = CreateWaiter(out var downloader, out var signalWaiter, videoDetails);

        (await waiter.WaitForStartAsync(videoId)).ShouldBe(expected);

        await downloader.Received(1).GetVideoDetailsAsync(videoId);
        await signalWaiter.DidNotReceive().WaitForCancelKeyPressAsync(Arg.Any<TimeSpan>(), Arg.Any<TimeProvider>());
    }

    public static IEnumerable<WaitForStartAsyncTestCase> WaitForStartAsyncTestCases()
    {
        var timeZones = new[] { TimeZoneInfo.Utc, TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo") };
        return timeZones.SelectMany(CreateWaitForStartAsyncTestCases);
    }

    public static IEnumerable<WaitForStartAsyncTestCase> CreateWaitForStartAsyncTestCases(TimeZoneInfo timeZone)
    {
        static DateTimeOffset Parse(string value) => DateTimeOffset.Parse($"2026-08-28T{value}Z");

        return
        [
            new WaitForStartAsyncTestCase(
                Parse("11:57:00"),
                timeZone,
                TimeSpan.FromMinutes(1),
                [
                    // 通常ケース(StartCheckBuffer有り): (is_upcoming, xxx) -> (is_upcoming, null) -> (is_live, xxx)
                    (CreateVideoDetails("is_upcoming", Parse("12:00:00")), TimeSpan.FromMinutes( 2)),
                    (CreateVideoDetails("is_upcoming", Parse("12:00:00")), TimeSpan.FromSeconds(30)),
                    (CreateVideoDetails("is_upcoming", null             ), TimeSpan.FromSeconds(30)),
                    (CreateVideoDetails("is_live",     Parse("12:00:00")), null),
                ],
                true),
            new WaitForStartAsyncTestCase(
                Parse("11:58:30"),
                timeZone,
                TimeSpan.FromMinutes(3),
                [
                    // 現在時刻が配信開始チェック時刻(配信開始時刻 - StartCheckBuffer)を過ぎているケース
                    (CreateVideoDetails("is_upcoming", Parse("12:00:00")), TimeSpan.FromSeconds(30)),
                    (CreateVideoDetails("is_live",     Parse("11:59:00")), null),
                ],
                true),
            new WaitForStartAsyncTestCase(
                Parse("11:57:00"),
                timeZone,
                TimeSpan.Zero,
                [
                    // 通常ケース(StartCheckBuffer無し): (is_upcoming, xxx) -> (is_upcoming, null) -> (is_live, xxx)
                    (CreateVideoDetails("is_upcoming", Parse("12:00:00")), TimeSpan.FromMinutes( 3)),
                    (CreateVideoDetails("is_upcoming", null             ), TimeSpan.FromSeconds(30)),
                    (CreateVideoDetails("is_live",     Parse("12:00:30")), null),
                ],
                true),
            new WaitForStartAsyncTestCase(
                Parse("11:57:00"),
                timeZone,
                TimeSpan.Zero,
                [
                    // 配信開始時刻より前に配信開始するケース: (is_upcoming, xxx) -> (is_live, xxx)
                    (CreateVideoDetails("is_upcoming", Parse("12:00:00")), TimeSpan.FromMinutes( 3)),
                    (CreateVideoDetails("is_live",     Parse("12:00:00")), null),
                ],
                true),
            new WaitForStartAsyncTestCase(
                Parse("11:57:00"),
                timeZone,
                TimeSpan.Zero,
                [
                    // 配信開始せずに非公開/削除されるケース: (is_upcoming, xxx) -> (null, null)
                    (CreateVideoDetails("is_upcoming", Parse("12:00:00")), TimeSpan.FromMinutes( 3)),
                    (CreateVideoDetails(null,          null             ), null),
                ],
                false),
        ];
    }

    [TestMethod]
    [DynamicData(nameof(WaitForStartAsyncTestCases))]
    public async Task WaitForStartAsync_ReturnsResult(WaitForStartAsyncTestCase testCase)
    {
        var videoId = "VideoID";
        var videoDetails = testCase.Steps.Select(t => t.VideoDetails).ToArray();
        var waitTimes = testCase.Steps.Select(t => t.WaitTime).OfType<TimeSpan>().ToArray();
        var callCounts = waitTimes.Distinct().ToDictionary(t => t, t => 0);

        var waiter = CreateWaiter(
            out var timeProvider,
            out var downloader,
            out var signalWaiter,
            videoDetails,
            testCase.StartCheckBuffer,
            TimeSpan.FromSeconds(30));

        timeProvider.SetUtcNow(testCase.Now);
        timeProvider.SetLocalTimeZone(testCase.TimeZone);

        using var semaphore = new SemaphoreSlim(0);
        signalWaiter.When(t => t.WaitForCancelKeyPressAsync(Arg.Any<TimeSpan>(), Arg.Any<TimeProvider>())).Do(_ => semaphore.Release());

        var task = waiter.WaitForStartAsync(videoId);

        var cancellationToken = TestContext.CancellationToken;
        foreach (var waitTime in waitTimes)
        {
            (await semaphore.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken)).ShouldBeTrue();

            await signalWaiter.Received(++callCounts[waitTime]).WaitForCancelKeyPressAsync(waitTime, Arg.Any<TimeProvider>());
            timeProvider.Advance(waitTime);
        }

        (await task).ShouldBe(testCase.Expected);

        await downloader.Received(videoDetails.Length).GetVideoDetailsAsync(videoId);
        await signalWaiter.Received(waitTimes.Length).WaitForCancelKeyPressAsync(Arg.Any<TimeSpan>(), Arg.Any<TimeProvider>());
    }

    private static string CreateVideoDetails(string? status, DateTimeOffset? timestamp)
    {
        return JsonSerializer.Serialize(
            new Dictionary<string, object?>
            {
                ["live_status"] = status,
                ["release_timestamp"] = timestamp?.ToUnixTimeSeconds(),
            });
    }

    private static BehaviorSettings CreateSettings(TimeSpan startCheckBuffer, TimeSpan startCheckInterval)
    {
        return new BehaviorSettings
        {
            StartCheckBufferSeconds = (int)startCheckBuffer.TotalSeconds,
            StartCheckIntervalSeconds = (int)startCheckInterval.TotalSeconds,

            // 以下は未使用
            DownloadAttempts = 0,
            ErrorRetryAttempts = 0,
            ErrorRetryIntervalSeconds = 0,
        };
    }

    private static YouTubeLiveStartWaiter CreateWaiter(string videoDetails)
    {
        return CreateWaiter(out _, out _, videoDetails);
    }

    private static YouTubeLiveStartWaiter CreateWaiter(
        out IVideoDownloader downloader,
        out IConsoleSignalWaiter signalWaiter,
        string videoDetails)
    {
        return CreateWaiter(out _, out downloader, out signalWaiter, [videoDetails], TimeSpan.Zero, TimeSpan.Zero);
    }

    private static YouTubeLiveStartWaiter CreateWaiter(
        out FakeTimeProvider timeProvider,
        out IVideoDownloader downloader,
        out IConsoleSignalWaiter signalWaiter,
        IReadOnlyList<string> videoDetails,
        TimeSpan startCheckBuffer,
        TimeSpan startCheckInterval)
    {
        downloader = Substitute.For<IVideoDownloader>();
        downloader.GetVideoDetailsAsync(Arg.Any<string>()).Returns(videoDetails);

        signalWaiter = Substitute.For<IConsoleSignalWaiter>();
        signalWaiter.WaitForCancelKeyPressAsync(Arg.Any<TimeSpan>(), Arg.Any<TimeProvider>())
            .Returns(x => Task.Delay((TimeSpan)x[0], (TimeProvider)x[1]));

        return new YouTubeLiveStartWaiter(
            Substitute.For<ILogger>(),
            timeProvider = new FakeTimeProvider(),
            CreateSettings(startCheckBuffer, startCheckInterval),
            downloader,
            signalWaiter);
    }

    public record WaitForStartAsyncTestCase(
        DateTimeOffset Now,
        TimeZoneInfo TimeZone,
        TimeSpan StartCheckBuffer,
        ImmutableArray<(string VideoDetails, TimeSpan? WaitTime)> Steps,
        bool Expected)
    {
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"\n{nameof(Now)} = {Now}");
            builder.Append(", ");
            builder.Append($"\n{nameof(TimeZone)} = {TimeZone}");
            builder.Append(", ");
            builder.Append($"\n{nameof(StartCheckBuffer)} = {StartCheckBuffer}");
            builder.Append(", ");
            builder.Append($"\n{nameof(Steps)} = [\n\t{string.Join(",\n\t", Steps)}\n]");
            builder.Append(", ");
            builder.Append($"\n{nameof(Expected)} = {Expected}");

            return true;
        }
    }
}
