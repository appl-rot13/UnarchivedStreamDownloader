namespace UnarchivedStreamDownloader.YouTube;

using System.Collections.Concurrent;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;

public class YouTubeVideoDownloadService(IYouTubeVideoSource source, IYouTubeVideoDownloader downloader)
{
    public async Task<bool[]> DownloadAllAsync(IEnumerable<string> channelIds)
    {
        var downloadTasks = await this.StartDownloadAsync(channelIds);
        return (await downloadTasks.WhenAll()).OfType<bool>().ToArray();
    }

    private async Task<IEnumerable<Task<bool?>>> StartDownloadAsync(IEnumerable<string> channelIds)
    {
        var downloadTasks = new ConcurrentBag<Task<bool?>>();

        await channelIds
            .Select(id => id.Trim())
            .Distinct()
            .Select(async channelId =>
            {
                await foreach (var video in source.EnumerateVideos(channelId))
                {
                    downloadTasks.Add(downloader.DownloadAsync(video));
                }
            })
            .WhenAll();

        return downloadTasks;
    }
}
