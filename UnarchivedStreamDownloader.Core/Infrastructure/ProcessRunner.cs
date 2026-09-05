namespace UnarchivedStreamDownloader.Core.Infrastructure;

using System.Diagnostics;

public class ProcessRunner(string filePath, bool useShellExecute, ILogger? logger = null) : IProcessRunner
{
    public ProcessResult Run(string arguments, bool redirectStandardOutput)
    {
        logger?.WriteLine($"Exec: {filePath} {arguments}");

        using var process = StartProcess(arguments, redirectStandardOutput);
        var standardOutput = redirectStandardOutput
            ? process.StandardOutput.ReadToEnd()
            : string.Empty;

        process.WaitForExit();

        logger?.WriteLine($"Exit: {process.ExitCode}");
        return new ProcessResult(process.ExitCode, standardOutput);
    }

    public async Task<ProcessResult> RunAsync(string arguments, bool redirectStandardOutput)
    {
        logger?.WriteLine($"Exec: {filePath} {arguments}");

        using var process = StartProcess(arguments, redirectStandardOutput);
        var standardOutput = redirectStandardOutput
            ? await process.StandardOutput.ReadToEndAsync()
            : string.Empty;

        await process.WaitForExitAsync();

        logger?.WriteLine($"Exit: {process.ExitCode}");
        return new ProcessResult(process.ExitCode, standardOutput);
    }

    private Process StartProcess(string arguments, bool redirectStandardOutput)
    {
        return Process.Start(
            new ProcessStartInfo
            {
                FileName = filePath,
                Arguments = arguments,
                UseShellExecute = useShellExecute,
                RedirectStandardOutput = redirectStandardOutput,
            }) ?? throw new InvalidOperationException($"'{filePath}' could not be started.");
    }
}
