namespace UnarchivedStreamDownloader.YouTube;

using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Core.YouTube;

public class YouTubeVideoDownloader(ILogger logger, ILockFactory lockFactory, IProcessRunner processRunner) : IYouTubeVideoDownloader
{
    public Task<bool?> DownloadAsync(YouTubeVideo video)
    {
        return Task.Run(() => this.Download(video));
    }

    public bool? Download(YouTubeVideo video)
    {
        try
        {
            using var lockObject = lockFactory.TryCreate($"{nameof(UnarchivedStreamDownloader)}.{video.Id}");
            if (lockObject == null)
            {
                return null;
            }

            logger.WriteLine(
                $"A video targeted for downloading has been found.\n"
                    + $"  Channel ID:   {video.Channel.Id}\n"
                    + $"  Channel Name: {video.Channel.Name}\n"
                    + $"  Video ID:     {video.Id}\n"
                    + $"  Video Title:  {video.Title}\n");

            var result = processRunner.Run(video.Id);
            logger.WriteLine($"{video.Id}: The download has {(result ? "been completed or canceled" : "failed")}.");

            return result;
        }
        catch (Exception e)
        {
            logger.WriteLine($"{video.Id}: {e}");
            return false;
        }
    }
}
