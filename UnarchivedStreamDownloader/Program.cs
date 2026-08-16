
using UnarchivedStreamDownloader.Core.Configuration;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;
using UnarchivedStreamDownloader.YouTube;

var logger = Logger.GetInstance();

try
{
    var appSettings = Configuration.Load<AppSettings>("appsettings.json");
    var searchSettings = appSettings.SearchSettings;
    var suppressHttpErrors = appSettings.BehaviorSettings.SuppressHttpErrors;

    var downloadService = new YouTubeVideoDownloadService(
        new YouTubeVideoSearcher(
            searchSettings,
            new YouTubeFeedReader(new HttpReader(new HttpClient()))),
        new YouTubeVideoDownloader(
            logger,
            new MutexGuardFactory(),
            new ProcessRunner("UnarchivedStreamDownloader.Worker.exe")));

    var results = await downloadService.DownloadAllAsync(searchSettings.ChannelIDs, suppressHttpErrors);
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
