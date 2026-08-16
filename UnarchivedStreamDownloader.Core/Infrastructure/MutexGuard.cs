
namespace UnarchivedStreamDownloader.Core.Infrastructure;

public sealed class MutexGuard : IDisposable
{
    private readonly Mutex mutex;

    private MutexGuard(Mutex mutex)
    {
        this.mutex = mutex;
    }

    public static MutexGuard? TryCreate(string name)
    {
        var mutex = new Mutex(true, name, out var created);
        if (!created)
        {
            return null;
        }

        return new MutexGuard(mutex);
    }

    public void Dispose()
    {
        this.mutex.ReleaseMutex();
        this.mutex.Dispose();
    }
}
