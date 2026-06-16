
namespace UnarchivedStreamDownloader.Core.Utilities;

public class HttpReader(HttpClient client) : IHttpReader
{
    public async Task<HttpResponseMessage> GetResponseAsync(string url)
    {
        return await client.GetAsync(url);
    }
}
