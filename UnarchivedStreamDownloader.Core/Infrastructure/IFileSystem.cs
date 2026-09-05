namespace UnarchivedStreamDownloader.Core.Infrastructure;

public interface IFileSystem
{
    IEnumerable<string> EnumerateFiles(string searchPattern, SearchOption searchOption);
}
