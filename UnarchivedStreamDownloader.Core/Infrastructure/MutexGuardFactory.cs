namespace UnarchivedStreamDownloader.Core.Infrastructure;

public class MutexGuardFactory : ILockFactory
{
    public IDisposable? TryCreate(string key)
    {
        return MutexGuard.TryCreate(key);
    }
}
