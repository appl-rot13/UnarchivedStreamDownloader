namespace UnarchivedStreamDownloader.Core.Infrastructure;

public record ProcessResult(int ExitCode, string StandardOutput = "")
{
    public bool IsSuccess => ExitCode == 0;
}
