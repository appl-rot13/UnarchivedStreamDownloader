
namespace UnarchivedStreamDownloader.Core.Configuration.Models;

public static class AppSettingsExtensions
{
    public static void PauseOptionally(this AppSettings appSettings)
    {
        if (appSettings.BehaviorSettings.PauseOnNormalExit)
        {
            Console.ReadLine();
        }
    }
}
