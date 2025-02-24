
using System.Diagnostics;

using UnarchivedStreamDownloader;
using UnarchivedStreamDownloader.Core.Configuration;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.Core.Utilities.Logging;

var logger = Logger.GetInstance();
var hasError = false;

try
{
    var appSettings = Configuration.Load<AppSettings>("appsettings.json");
    var searchSettings = appSettings.SearchSettings;

    await searchSettings.ChannelIDs
        .Select(channelId => channelId.Trim())
        .Distinct()
        .AsParallel()
        .SelectMany(YouTubeDataRetriever.EnumerateLatestVideos)
        .Where(video => searchSettings.IsMatch(video.Title, video.Description))
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

                        if (!WaitForDownload(video.Id))
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

return;

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
