namespace UnarchivedStreamDownloader.WorkerTest;

using System.Collections.Immutable;
using System.Text;
using NSubstitute;
using Shouldly;
using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Worker;

[TestClass]
public class VideoDownloaderTest
{
    public static IEnumerable<ArchiveFileExistsTestCase> ArchiveFileExistsTestCases()
    {
        return
        [
            new ArchiveFileExistsTestCase("Video-ID", [], false),
            new ArchiveFileExistsTestCase("Video-ID", ["Title [Video-ID].mkv"], true),
            new ArchiveFileExistsTestCase("Video-ID", ["Title 2026-08-25 12_00 [Video-ID].mkv"], false),
            new ArchiveFileExistsTestCase("Video-ID", ["Title [Video-ID].mkv", "Title 2026-08-25 12_00 [Video-ID].mkv"], true),
            new ArchiveFileExistsTestCase("Video.ID", ["Title [Video.ID].mkv"], true),
        ];
    }

    [TestMethod]
    [DynamicData(nameof(ArchiveFileExistsTestCases))]
    public void ArchiveFileExists_ReturnsResult(ArchiveFileExistsTestCase testCase)
    {
        var basePath = Path.GetTempPath();
        var files = testCase.FileNames.Select(fileName => Path.Combine(basePath, fileName));
        var downloader = CreateDownloader(files);

        downloader.ArchiveFileExists(testCase.VideoId).ShouldBe(testCase.ExpectedResult);
    }

    [TestMethod]
    [DataRow(@"")]
    [DataRow(@"{}")]
    [DataRow(@"{""id"":""Video-ID""}")]
    public async Task GetVideoDetailsAsync_ReturnsOutput(string output)
    {
        var videoId = "Video-ID";
        var downloader = CreateDownloader(out var processRunner, false, true, output, []);

        (await downloader.GetVideoDetailsAsync(videoId)).ShouldBe(output);

        var arguments = $"--ignore-no-formats-error --no-warnings --dump-json -- {videoId}";
        await processRunner.Received(1).RunAsync(arguments, true);
    }

    [TestMethod]
    public async Task GetVideoDetailsAsync_SignalIsSet_ThrowsOperationCanceledException()
    {
        var downloader = CreateDownloader(out _, true, true, string.Empty, []);
        Should.Throw<OperationCanceledException>(async () => await downloader.GetVideoDetailsAsync("Video-ID"));
    }

    [TestMethod]
    [DataRow(false, @"-- Video-ID")]
    [DataRow( true, @"-- Video-ID")]
    [DataRow(false, @"--verbose -- Video-ID", "--verbose")]
    [DataRow( true, @"--verbose -- Video-ID", "--verbose", "--wait-for-video 30")]
    [DataRow(false, @"--verbose --cookies ""cookies.txt"" -- Video-ID", "--verbose", "--wait-for-video 30", @"--cookies ""cookies.txt""")]
    public async Task DownloadAsync_ReturnsResult(bool result, string arguments, params string[] options)
    {
        var videoId = "Video-ID";
        var downloader = CreateDownloader(out var processRunner, false, result, string.Empty, options);

        (await downloader.DownloadAsync(videoId)).ShouldBe(result);
        await processRunner.Received(1).RunAsync(arguments, false);
    }

    [TestMethod]
    public async Task DownloadAsync_SignalIsSet_ThrowsOperationCanceledException()
    {
        var downloader = CreateDownloader(out _, true, true, string.Empty, []);
        Should.Throw<OperationCanceledException>(async () => await downloader.DownloadAsync("Video-ID"));
    }

    private static VideoDownloader CreateDownloader(IEnumerable<string> files)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.EnumerateFiles(Arg.Any<string>(), Arg.Any<SearchOption>()).Returns(files);

        return new VideoDownloader(
            fileSystem,
            Substitute.For<IProcessRunner>(),
            () => Substitute.For<IAsyncSignal>(),
            []);
    }

    private static VideoDownloader CreateDownloader(
        out IProcessRunner processRunner,
        bool isSet,
        bool processResult,
        string processOutput,
        IReadOnlyCollection<string> options)
    {
        processRunner = Substitute.For<IProcessRunner>();
        processRunner.RunAsync(Arg.Any<string>(), Arg.Any<bool>()).Returns(new ProcessResult(processResult ? 0 : 1, processOutput));

        var signal = Substitute.For<IAsyncSignal>();
        signal.IsSet.Returns(isSet);

        return new VideoDownloader(
            Substitute.For<IFileSystem>(),
            processRunner,
            () => signal,
            options);
    }

    public record ArchiveFileExistsTestCase(string VideoId, ImmutableArray<string> FileNames, bool ExpectedResult)
    {
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            builder.Append($"{nameof(VideoId)} = {VideoId}");
            builder.Append(", ");
            builder.Append($"{nameof(FileNames)} = [{string.Join(", ", FileNames)}]");
            builder.Append(", ");
            builder.Append($"{nameof(ExpectedResult)} = {ExpectedResult}");

            return true;
        }
    }
}
