
using UnarchivedStreamDownloader.Configuration;
using UnarchivedStreamDownloader.Configuration.Models;
using UnarchivedStreamDownloader.Utilities;
using UnarchivedStreamDownloader.Utilities.Extensions;
using UnarchivedStreamDownloader.Utilities.Logging;

var logger = Logger.GetInstance();
var hasError = false;

try
{
    var appSettings = Configuration.Load<AppSettings>("appsettings.json");
    var downloader = new Downloader(logger, appSettings.DownloaderSettings);

    var searchSettings = appSettings.SearchSettings;
    await searchSettings.ChannelIDs
        .Select(channelId => channelId.Trim())
        .Distinct()
        .AsParallel()
        .SelectMany(YouTubeDataRetriever.EnumerateLatestVideos)
        .Where(video => video.Title.ContainsAny(searchSettings.Keywords, StringComparison.OrdinalIgnoreCase))
        .Select(
            video => Task.Run(() =>
            {
                try
                {
                    using var mutex = new Mutex(true, $"{nameof(UnarchivedStreamDownloader)}.{video.Id}", out var created);
                    if (!created)
                    {
                        return;
                    }

                    try
                    {
                        logger.WriteLine(
                            $"A video targeted for downloading has been found.\n"
                            + $"  Channel ID:   {video.Channel.Id}\n"
                            + $"  Channel Name: {video.Channel.Name}\n"
                            + $"  Video ID:     {video.Id}\n"
                            + $"  Video Title:  {video.Title}\n");

                        if (!downloader.TwoStepDownloadAsync(video.Id).GetAwaiter().GetResult())
                        {
                            hasError = true;
                        }
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
                catch (Exception e)
                {
                    logger.WriteLine($"[{video.Id}] {e}");
                    hasError = true;
                }
            }))
        .WhenAll();
}
catch (Exception e)
{
    logger.WriteLine($"{e}");
    hasError = true;
}

if (hasError)
{
    Console.ReadLine();
}
