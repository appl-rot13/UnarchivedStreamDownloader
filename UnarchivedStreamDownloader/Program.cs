
using System.Diagnostics;

using UnarchivedStreamDownloader;
using UnarchivedStreamDownloader.Core.Configuration;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.Core.Utilities.Logging;

var logger = Logger.GetInstance();

try
{
    var appSettings = Configuration.Load<AppSettings>("appsettings.json");
    var searchSettings = appSettings.SearchSettings;

    var results = await searchSettings.ChannelIDs
        .Select(channelId => channelId.Trim())
        .Distinct()
        .AsParallel()
        .SelectMany(YouTubeDataRetriever.EnumerateLatestVideos)
        .Where(video => searchSettings.IsMatch(video.Title, video.Description))
        .Select(DownloadAsync)
        .WhenAll();

    if (results.AllTrue())
    {
        return;
    }
}
catch (Exception e)
{
    logger.WriteLine($"{e}");
}

Console.ReadLine();
return;

Task<bool> DownloadAsync(((string Id, string Name) Channel, string Id, string Title, string Description) video)
{
    return Task.Run(() =>
    {
        try
        {
            using var mutex = new Mutex(true, $"{nameof(UnarchivedStreamDownloader)}.{video.Id}", out var created);
            if (!created)
            {
                return true;
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
                logger.WriteLine($"{video.Id}: The download has been ended or canceled. (Success: {result})");

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
