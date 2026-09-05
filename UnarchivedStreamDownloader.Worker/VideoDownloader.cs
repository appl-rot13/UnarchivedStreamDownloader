namespace UnarchivedStreamDownloader.Worker;

using System.Text.RegularExpressions;
using UnarchivedStreamDownloader.Core.Infrastructure;

public class VideoDownloader(
    IFileSystem fileSystem,
    IProcessRunner processRunner,
    Func<IAsyncSignal> createCancelSignal,
    IReadOnlyCollection<string> options) : IVideoDownloader
{
    public bool ArchiveFileExists(string videoId)
    {
        return fileSystem.EnumerateFiles($"*[{videoId}].*", SearchOption.TopDirectoryOnly)
            .Any(filePath => !Regex.IsMatch(filePath, $@"\d{{4}}-\d{{2}}-\d{{2}} \d{{2}}_\d{{2}} \[{Regex.Escape(videoId)}\]\."));
    }

    public async Task<string> GetVideoDetailsAsync(string videoId)
    {
        var arguments = CreateArguments(
            videoId,
            "--ignore-no-formats-error",
            "--no-warnings",
            "--dump-json");

        var result = await this.RunAsync(arguments, true);
        return result.StandardOutput;
    }

    public async Task<bool> DownloadAsync(string videoId)
    {
        var arguments = CreateArguments(videoId, options);
        arguments = ExcludeWaitForVideoOption(arguments);

        var result = await this.RunAsync(arguments, false);
        return result.IsSuccess;
    }

    private async Task<ProcessResult> RunAsync(string arguments, bool redirectStandardOutput)
    {
        using var cancelSignal = createCancelSignal();
        var result = await processRunner.RunAsync(arguments, redirectStandardOutput);

        return cancelSignal.IsSet ? throw new OperationCanceledException() : result;
    }

    private static string CreateArguments(string videoId, params IEnumerable<string> options)
    {
        return string.Join(' ', options.Select(option => option.Trim())) + $" -- {videoId}";
    }

    private static string ExcludeWaitForVideoOption(string arguments)
    {
        return Regex.Replace(arguments, @"--wait-for-video \S+ ", string.Empty).Trim();
    }
}
