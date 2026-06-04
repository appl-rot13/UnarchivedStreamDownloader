
using System.Collections.Concurrent;
using System.Diagnostics;

using UnarchivedStreamDownloader.Core.Configuration;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Utilities;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.Core.Utilities.Logging;
using UnarchivedStreamDownloader.YouTube;

var logger = Logger.GetInstance();

try
{
    var appSettings = Configuration.Load<AppSettings>("appsettings.json");
    var searchSettings = appSettings.SearchSettings;
    var suppressHttpErrors = appSettings.BehaviorSettings.SuppressHttpErrors;

    var youtube = new YouTubeDataRetriever(new HttpReader(new HttpClient()));
    var downloadTasks = new ConcurrentBag<Task<bool?>>();

    await searchSettings.ChannelIDs
        .Select(id => id.Trim())
        .Distinct()
        .Select(async channelId =>
        {
            await foreach (var video in youtube.EnumerateLatestVideos(channelId, suppressHttpErrors))
            {
                if (searchSettings.IsMatch(video.Title, video.Description))
                {
                    downloadTasks.Add(DownloadAsync(video));
                }
            }
        }).WhenAll();

    var results = (await downloadTasks.WhenAll()).OfType<bool>().ToArray();
    if (results.IsNullOrEmpty())
    {
        return;
    }

    if (results.IsAllTrue())
    {
        logger.WriteLine("All downloads have been completed or canceled.");
        appSettings.PauseOptionally();
        return;
    }
}
catch (Exception e)
{
    logger.WriteLine($"{e}");
}

logger.WriteLine("Some downloads have failed.");
Console.ReadLine();
return;

Task<bool?> DownloadAsync(YouTubeVideo video)
{
    return Task.Run<bool?>(() =>
    {
        try
        {
            using var mutex = new Mutex(true, $"{nameof(UnarchivedStreamDownloader)}.{video.Id}", out var created);
            if (!created)
            {
                return null;
            }

            try
            {
                logger.WriteLine(
                    $"A video targeted for downloading has been found.\n"
                    + $"  Channel ID:   {video.Channel.Id}\n"
                    + $"  Channel Name: {video.Channel.Name}\n"
                    + $"  Video ID:     {video.Id}\n"
                    + $"  Video Title:  {video.Title}\n");

                var result = WaitForDownload(video.Id);
                logger.WriteLine($"{video.Id}: The download has {(result ? "been completed or canceled" : "failed")}.");

                return result;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        catch (Exception e)
        {
            logger.WriteLine($"{video.Id}: {e}");
            return false;
        }
    });
}

bool WaitForDownload(string videoId)
{
    var filePath = "UnarchivedStreamDownloader.Worker.exe";
    var process = Process.Start(
        new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = videoId,
                UseShellExecute = true,
            });
    if (process == null)
    {
        throw new InvalidOperationException($"'{filePath}' could not be started.");
    }

    process.WaitForExit();
    return process.ExitCode == 0;
}
