namespace UnarchivedStreamDownloader.WorkerTest;

using NSubstitute;
using Shouldly;
using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Worker;

[TestClass]
public class VideoDownloaderTest
{
    [TestMethod]
    [DataRow("Video-ID", new string[] {                                                                 }, false)]
    [DataRow("Video-ID", new string[] { "Title [Video-ID].mkv"                                          },  true)]
    [DataRow("Video-ID", new string[] { "Title 2026-08-25 12_00 [Video-ID].mkv"                         }, false)]
    [DataRow("Video-ID", new string[] { "Title [Video-ID].mkv", "Title 2026-08-25 12_00 [Video-ID].mkv" },  true)]
    [DataRow("Video.ID", new string[] { "Title [Video.ID].mkv"                                          },  true)]
    public void ArchiveFileExists_ReturnsResult(string videoId, IEnumerable<string> fileNames, bool expectedResult)
    {
        var basePath = Path.GetTempPath();
        var files = fileNames.Select(fileName => Path.Combine(basePath, fileName));
        var downloader = CreateDownloader(files);

        downloader.ArchiveFileExists(videoId).ShouldBe(expectedResult);
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
    public async Task GetVideoDetailsAsync_SignalIsTriggered_ThrowsOperationCanceledException()
    {
        var downloader = CreateDownloader(out _, true, true, string.Empty, []);
        await Should.ThrowAsync<OperationCanceledException>(() => downloader.GetVideoDetailsAsync("Video-ID"));
    }

    [TestMethod]
    [DataRow(new string[] {                                                                  }, @"-- Video-ID",                                     false)]
    [DataRow(new string[] {                                                                  }, @"-- Video-ID",                                      true)]
    [DataRow(new string[] { "--verbose"                                                      }, @"--verbose -- Video-ID",                           false)]
    [DataRow(new string[] { "--verbose", "--wait-for-video 30"                               }, @"--verbose -- Video-ID",                            true)]
    [DataRow(new string[] { "--verbose", "--wait-for-video 30", @"--cookies ""cookies.txt""" }, @"--verbose --cookies ""cookies.txt"" -- Video-ID", false)]
    public async Task DownloadAsync_ReturnsResult(IReadOnlyCollection<string> options, string arguments, bool result)
    {
        var videoId = "Video-ID";
        var downloader = CreateDownloader(out var processRunner, false, result, string.Empty, options);

        (await downloader.DownloadAsync(videoId)).ShouldBe(result);
        await processRunner.Received(1).RunAsync(arguments, false);
    }

    [TestMethod]
    public async Task DownloadAsync_SignalIsTriggered_ThrowsOperationCanceledException()
    {
        var downloader = CreateDownloader(out _, true, true, string.Empty, []);
        await Should.ThrowAsync<OperationCanceledException>(() => downloader.DownloadAsync("Video-ID"));
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
        bool isTriggered,
        bool processResult,
        string processOutput,
        IReadOnlyCollection<string> options)
    {
        processRunner = Substitute.For<IProcessRunner>();
        processRunner.RunAsync(Arg.Any<string>(), Arg.Any<bool>()).Returns(new ProcessResult(processResult ? 0 : 1, processOutput));

        var signal = Substitute.For<IAsyncSignal>();
        signal.IsTriggered.Returns(isTriggered);

        return new VideoDownloader(
            Substitute.For<IFileSystem>(),
            processRunner,
            () => signal,
            options);
    }
}
