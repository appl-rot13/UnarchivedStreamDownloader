namespace UnarchivedStreamDownloader.YouTube;

using System.Collections.Concurrent;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;

public class YouTubeVideoDownloadService(IYouTubeVideoSource source, IYouTubeVideoDownloader downloader)
{
    public async Task<bool[]> DownloadAllAsync(IEnumerable<string> channelIds, bool suppressHttpErrors)
    {
        var downloadTasks = await this.StartDownloadAsync(channelIds, suppressHttpErrors);
        return (await downloadTasks.WhenAll()).OfType<bool>().ToArray();
    }

    private async Task<IEnumerable<Task<bool?>>> StartDownloadAsync(IEnumerable<string> channelIds, bool suppressHttpErrors)
    {
        var downloadTasks = new ConcurrentBag<Task<bool?>>();

        await channelIds
            .Select(id => id.Trim())
            .Distinct()
            .Select(async channelId =>
            {
                await foreach (var video in source.EnumerateVideos(channelId, suppressHttpErrors))
                {
                    downloadTasks.Add(downloader.DownloadAsync(video));
                }
            })
            .WhenAll();

        return downloadTasks;
    }
}
