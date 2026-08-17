namespace UnarchivedStreamDownloader.Core.Infrastructure;

using System.Diagnostics;

public class ProcessRunner(string filePath) : IProcessRunner
{
    public bool Run(string arguments)
    {
        var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = arguments,
                UseShellExecute = true,
            });
        if (process == null)
        {
            throw new InvalidOperationException($"'{filePath}' could not be started.");
        }

        process.WaitForExit();
        return process.ExitCode == 0;
    }
}
