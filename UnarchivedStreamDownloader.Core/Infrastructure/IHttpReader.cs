
namespace UnarchivedStreamDownloader.Core.Infrastructure;

public interface IHttpReader
{
    public Task<HttpResponseMessage> GetResponseAsync(string url);
}
