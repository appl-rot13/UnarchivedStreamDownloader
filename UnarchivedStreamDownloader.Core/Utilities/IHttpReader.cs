
namespace UnarchivedStreamDownloader.Core.Utilities;

public interface IHttpReader
{
    public Task<HttpResponseMessage> GetResponseAsync(string url);
}
