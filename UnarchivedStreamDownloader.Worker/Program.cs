using UnarchivedStreamDownloader.Core.Configuration;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Infrastructure;
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

    var downloaderSettings = appSettings.DownloaderSettings;
    var behaviorSettings = appSettings.BehaviorSettings;

    var downloader = new VideoDownloader(
        new FileSystem(),
        new ProcessRunner(downloaderSettings.FilePath, false, logger),
        () => new ConsoleCancelKeyPressSignal(),
        downloaderSettings.Options);
    var downloadService = new VideoDownloadService(
        logger,
        TimeProvider.System,
        behaviorSettings,
        downloader,
        new YouTubeLiveStartWaiter(
            logger,
            TimeProvider.System,
            behaviorSettings,
            downloader,
            new ConsoleSignalWaiter()));

    if (await downloadService.DownloadArchiveAsync(videoId))
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
