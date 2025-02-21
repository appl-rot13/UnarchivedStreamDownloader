
namespace UnarchivedStreamDownloader.Worker;

using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.Core.Utilities.Logging;

public class Downloader(ILogger? logger, DownloaderSettings settings)
{
    private static readonly int DownloadAttempts = 10;

    private static readonly int ErrorRetryAttempts = 3;

    private static readonly TimeSpan ErrorRetryInterval = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan StartCheckBuffer = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan StartCheckInterval = TimeSpan.FromMinutes(1);

    public Task<bool> DownloadArchiveAsync(string videoId) =>
        this.DownloadArchiveAsync(videoId, DownloadAttempts);

    public async Task<bool> DownloadArchiveAsync(string videoId, int count)
    {
        if (!await this.WaitForStartAsync(videoId))
        {
            // 配信が削除された場合
            return false;
        }

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
        ConsoleCancelEventHandler handler = (_, e) => e.Cancel = true;
        Console.CancelKeyPress += handler;

        try
        {
            var process = this.StartDownloader(videoId, settings.Options);

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    private async Task<bool> WaitForStartAsync(string videoId)
    {
        const string statusKey = "live_status";
        const string timestampKey = "release_timestamp";

        while (true)
        {
            var videoDetails = await this.GetVideoDetailsAsync(videoId);
            if (!videoDetails.ContainsKey(statusKey) || !videoDetails.ContainsKey(timestampKey))
            {
                throw new InvalidOperationException($"Unexpected object: {videoDetails}");
            }

            var status = videoDetails[statusKey]?.GetValue<string>();
            var timestamp = videoDetails[timestampKey]?.GetValue<int>();
            if (status == null && timestamp == null)
            {
                // 配信が削除された場合
                logger?.WriteLine("The video is either private or has been removed.");
                return false;
            }

            if (status != "is_upcoming")
            {
                // 配信が開始した場合
                logger?.WriteLine("The video has started.");
                return true;
            }

            var scheduledStartTime = DateTime.Now;
            if (timestamp.HasValue)
            {
                scheduledStartTime = DateTimeOffset.FromUnixTimeSeconds(timestamp.Value).LocalDateTime;
                logger?.WriteLine($"The video is scheduled to start at {scheduledStartTime}.");
            }

            var attemptTime = scheduledStartTime.Subtract(StartCheckBuffer);
            var timeRemaining = (attemptTime - DateTime.Now).TruncateToSeconds();
            if (timeRemaining <= TimeSpan.Zero)
            {
                // 配信開始直前の場合
                timeRemaining = StartCheckInterval;
                attemptTime = DateTime.Now.Add(timeRemaining);
            }

            logger?.WriteLine($"Wait until {attemptTime} (Time remaining: {timeRemaining}) - Press Ctrl+C to try now.");
            using var cts = new CancellationTokenSource(timeRemaining);
            await ConsoleHelper.WaitForCancelKeyPress(cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private async Task<JsonObject> GetVideoDetailsAsync(string videoId)
    {
        var process = this.StartDownloader(
            videoId,
            startInfo => startInfo.RedirectStandardOutput = true,
            "--ignore-no-formats-error",
            "--no-warnings",
            "--dump-json");

        var jsonString = await process.StandardOutput.ReadToEndAsync();
        var jsonObject = JsonNode.Parse(jsonString)?.AsObject();
        if (jsonObject == null)
        {
            throw new InvalidOperationException($"Unexpected output: {jsonString}");
        }

        await process.WaitForExitAsync();
        return jsonObject;
    }

    private Process StartDownloader(
        string videoId,
        params IEnumerable<string> options) =>
        this.StartDownloader(videoId, _ => { }, options);

    private Process StartDownloader(
        string videoId,
        Action<ProcessStartInfo> startInfoSetting,
        params IEnumerable<string> options)
    {
        var filePath = settings.FilePath;
        var arguments = CreateArguments(videoId, options);
        logger?.WriteLine($"Exec: {filePath} {arguments}");

        var startInfo =
            new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                };
        startInfoSetting(startInfo);

        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"'{filePath}' could not be started.");
        }

        EventHandler? handler = null;
        handler = (_, _) =>
        {
            process.Exited -= handler;
            logger?.WriteLine($"Exit: {process.ExitCode}");
        };

        process.Exited += handler;
        return process;
    }

    private static string CreateArguments(string videoId, params IEnumerable<string> options)
    {
        var arguments = string.Join(' ', options.Select(option => option.Trim())) + $" -- {videoId}";
        return Regex.Replace(arguments, @"--wait-for-video \S+ ", string.Empty).Trim();
    }

    private static bool ArchiveFileExists(string videoId)
    {
        return Directory.EnumerateFiles(Directory.GetCurrentDirectory(), $"*[{videoId}].*", SearchOption.TopDirectoryOnly)
            .Any(filePath => !Regex.IsMatch(filePath, $@"\d{{4}}-\d{{2}}-\d{{2}} \d{{2}}_\d{{2}} \[{videoId}\]\."));
    }
}
