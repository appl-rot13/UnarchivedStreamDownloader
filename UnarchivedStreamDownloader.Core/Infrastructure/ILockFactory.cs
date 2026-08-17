namespace UnarchivedStreamDownloader.Core.Infrastructure;

public interface ILockFactory
{
    IDisposable? TryCreate(string key);
}
