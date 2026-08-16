
namespace UnarchivedStreamDownloader.Core.Infrastructure;

public interface ILogger
{
    public void WriteLine();

    public void WriteLine(string message);
}
