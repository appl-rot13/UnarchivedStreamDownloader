namespace UnarchivedStreamDownloader.Worker;

using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Infrastructure;

public class VideoDownloadService(
    ILogger logger,
    TimeProvider timeProvider,
    BehaviorSettings behavior,
    IVideoDownloader downloader,
    IYouTubeLiveStartWaiter startWaiter)
{
    public Task<bool> DownloadArchiveAsync(string videoId)
    {
        return this.DownloadArchiveAsync(videoId, behavior.DownloadAttempts);
    }

    public async Task<bool> DownloadArchiveAsync(string videoId, int count)
    {
        if (downloader.ArchiveFileExists(videoId))
        {
            logger.WriteLine("The video has already been downloaded.");
            return true;
        }

        if (!await startWaiter.WaitForStartAsync(videoId))
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

            if (downloader.ArchiveFileExists(videoId))
            {
                return true;
            }

            if (i < count)
            {
                logger.WriteLine($"Retry until the archive is downloaded. Attempt {i + 1}/{count}.");
            }
        }

        return false;
    }

    public Task<bool> DownloadWithRetryAsync(string videoId)
    {
        return this.DownloadWithRetryAsync(videoId, behavior.ErrorRetryAttempts);
    }

    public async Task<bool> DownloadWithRetryAsync(string videoId, int count)
    {
        for (var i = 1; i <= count; i++)
        {
            if (await downloader.DownloadAsync(videoId))
            {
                return true;
            }

            if (i < count)
            {
                await Task.Delay(behavior.ErrorRetryInterval, timeProvider);
                logger.WriteLine($"Retry the download due to an error. Attempt {i + 1}/{count}.");
            }
        }

        return false;
    }
}
