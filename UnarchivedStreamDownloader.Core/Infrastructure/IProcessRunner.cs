namespace UnarchivedStreamDownloader.Core.Infrastructure;

public interface IProcessRunner
{
    ProcessResult Run(string arguments, bool redirectStandardOutput);

    Task<ProcessResult> RunAsync(string arguments, bool redirectStandardOutput);
}
