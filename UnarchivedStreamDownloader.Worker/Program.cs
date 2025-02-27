
using UnarchivedStreamDownloader.Core.Utilities.Logging;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Configuration;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.Worker;

if (args.IsNullOrEmpty())
{
    return 2;
}

var logger = Logger.GetInstance();
var appSettings = Configuration.Load<AppSettings>("appsettings.json");

try
{
    var videoId = args[0];
    Console.Title = videoId;

    var downloader = new Downloader(logger, appSettings.DownloaderSettings, appSettings.BehaviorSettings);
    if (await downloader.DownloadArchiveAsync(videoId))
    {
        logger.WriteLine("The download has been completed.");
        appSettings.PauseOptionally();
        return 0;
    }
}
catch (OperationCanceledException)
{
    logger.WriteLine("The download has been canceled.");
    appSettings.PauseOptionally();
    return 0;
}
catch (Exception e)
{
    logger.WriteLine($"{e}");
}

logger.WriteLine("The download has failed.");
Console.ReadLine();
return 1;
