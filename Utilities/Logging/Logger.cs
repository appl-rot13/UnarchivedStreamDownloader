
namespace UnarchivedStreamDownloader.Utilities.Logging;

using System.Text;

public class Logger : ILogger
{
    private static readonly Lazy<ILogger> Instance = new(() => new Logger());

    private Logger() =>
        Console.OutputEncoding = Encoding.UTF8;

    public static ILogger GetInstance() => Instance.Value;

    public void WriteLine() =>
        Console.WriteLine();

    public void WriteLine(string message) =>
        Console.WriteLine($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}] {message}");
}
