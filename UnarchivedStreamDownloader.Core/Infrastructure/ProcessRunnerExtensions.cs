namespace UnarchivedStreamDownloader.Core.Infrastructure;

public static class ProcessRunnerExtensions
{
    public static bool Run(this IProcessRunner processRunner, string arguments)
    {
        return processRunner.Run(arguments, false).IsSuccess;
    }

    public static async Task<bool> RunAsync(this IProcessRunner processRunner, string arguments)
    {
        return (await processRunner.RunAsync(arguments, false)).IsSuccess;
    }
}
