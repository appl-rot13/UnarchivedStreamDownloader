
namespace UnarchivedStreamDownloader.Utilities;

using System.Diagnostics;

using UnarchivedStreamDownloader.Configuration.Models;
using UnarchivedStreamDownloader.Utilities.Logging;

public class Downloader(ILogger? logger, DownloaderSettings settings)
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(1);

    private static readonly TimeSpan LiveDetectionThreshold = TimeSpan.FromMinutes(30);
    
    private static readonly int TotalAttempts = 10;

    public async Task<bool> TwoStepDownloadAsync(string videoId)
    {
        var startTime = DateTime.Now;

        logger?.WriteLine($"[{videoId}] Start the first download.");
        var ret = await this.DownloadAsync(videoId);
        if (!ret)
        {
            // この時点で異常終了の場合、終了すべき？
        }

        var elapsed = DateTime.Now - startTime;
        if (elapsed > LiveDetectionThreshold)
        {
            // 初回ダウンロードに時間が掛かった場合、
            // ライブ配信だったと判断し、アーカイブのダウンロードを試行
            logger?.WriteLine($"[{videoId}] Start the second download.");
            ret &= await this.DownloadWithRetryAsync(videoId, TotalAttempts);
        }

        return ret;
    }

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
                await Task.Delay(RetryInterval);
                logger?.WriteLine($"[{videoId}] Retry the download. Attempt {i + 1}/{count}.");
            }
        }

        return false;
    }

    public async Task<bool> DownloadAsync(string videoId)
    {
        var arguments = string.Join(' ', settings.Options) + $" -- {videoId}";
        logger?.WriteLine($"[{videoId}] Exec: {settings.FilePath} {arguments}");

        var process = Process.Start(
            new ProcessStartInfo
                {
                    FileName = settings.FilePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                });
        if (process == null)
        {
            throw new InvalidOperationException("The downloader could not be started.");
        }

        await process.WaitForExitAsync();

        logger?.WriteLine($"[{videoId}] Exit: {process.ExitCode}");
        return process.ExitCode == 0;
    }
}
