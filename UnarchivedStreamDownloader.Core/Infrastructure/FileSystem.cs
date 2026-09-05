namespace UnarchivedStreamDownloader.Core.Infrastructure;

public class FileSystem : IFileSystem
{
    public IEnumerable<string> EnumerateFiles(string searchPattern, SearchOption searchOption)
    {
        return Directory.EnumerateFiles(Directory.GetCurrentDirectory(), searchPattern, searchOption);
    }
}
