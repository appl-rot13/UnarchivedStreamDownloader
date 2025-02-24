
using UnarchivedStreamDownloader.Core.Utilities.Logging;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Configuration;
using UnarchivedStreamDownloader.Worker;

if (args.Length < 1)
{
    return 2;
}

var logger = Logger.GetInstance();

try
{
    var videoId = args[0];
    Console.Title = videoId;

    var appSettings = Configuration.Load<AppSettings>("appsettings.json");
    var downloader = new Downloader(logger, appSettings.DownloaderSettings, appSettings.BehaviorSettings);

    if (await downloader.DownloadArchiveAsync(videoId))
    {
        return 0;
    }
}
catch (OperationCanceledException)
{
    return 0;
}
catch (Exception e)
{
    logger.WriteLine($"{e}");
}

Console.ReadLine();
return 1;
