
namespace UnarchivedStreamDownloader.Core.Utilities;

public interface IHttpReader
{
    public Task<string> GetResponseAsync(string url);
}
