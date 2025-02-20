
namespace UnarchivedStreamDownloader.Worker;

using System.Diagnostics;
using System.Text.RegularExpressions;

using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Utilities.Logging;

public class Downloader(ILogger? logger, DownloaderSettings settings)
{
    private static readonly int DownloadAttempts = 10;

    private static readonly int ErrorRetryAttempts = 3;

    private static readonly TimeSpan ErrorRetryInterval = TimeSpan.FromSeconds(1);

    public Task<bool> DownloadArchiveAsync(string videoId) =>
        this.DownloadArchiveAsync(videoId, DownloadAttempts);

    public async Task<bool> DownloadArchiveAsync(string videoId, int count)
    {
        logger?.WriteLine("Start the download.");

        for (var i = 1; i <= count; i++)
        {
            if (!await this.DownloadWithRetryAsync(videoId))
            {
                return false;
            }

            if (ArchiveFileExists(videoId))
            {
                return true;
            }

            if (i < count)
            {
                logger?.WriteLine($"Retry until the archive is downloaded. Attempt {i + 1}/{count}.");
            }
        }

        return false;
    }

    public Task<bool> DownloadWithRetryAsync(string videoId) =>
        this.DownloadWithRetryAsync(videoId, ErrorRetryAttempts);

    public async Task<bool> DownloadWithRetryAsync(string videoId, int count)
    {
        for (var i = 1; i <= count; i++)
        {
            if (await this.DownloadAsync(videoId))
            {
                return true;
            }

            if (i < count)
            {
                await Task.Delay(ErrorRetryInterval);
                logger?.WriteLine($"Retry the download due to an error. Attempt {i + 1}/{count}.");
            }
        }

        return false;
    }

    public async Task<bool> DownloadAsync(string videoId)
    {
        var filePath = settings.FilePath;
        var arguments = string.Join(' ', settings.Options) + $" -- {videoId}";
        logger?.WriteLine($"Exec: {filePath} {arguments}");

        ConsoleCancelEventHandler handler = (_, e) => e.Cancel = true;
        Console.CancelKeyPress += handler;

        try
        {
            var process = Process.Start(
                new ProcessStartInfo
                    {
                        FileName = filePath,
                        Arguments = arguments,
                        UseShellExecute = false,
                    });
            if (process == null)
            {
                throw new InvalidOperationException($"'{filePath}' could not be started.");
            }

            await process.WaitForExitAsync();

            logger?.WriteLine($"Exit: {process.ExitCode}");
            return process.ExitCode == 0;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    private static bool ArchiveFileExists(string videoId)
    {
        return Directory.EnumerateFiles(Directory.GetCurrentDirectory(), $"*[{videoId}].*", SearchOption.TopDirectoryOnly)
            .Any(filePath => !Regex.IsMatch(filePath, $@"\d{{4}}-\d{{2}}-\d{{2}} \d{{2}}_\d{{2}} \[{videoId}\]\."));
    }
}
