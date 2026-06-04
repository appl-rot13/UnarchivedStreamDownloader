
namespace UnarchivedStreamDownloader.Core.Utilities;

public class HttpReader(HttpClient client) : IHttpReader
{
    public async Task<string> GetResponseAsync(string url)
    {
        var response = await client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }
}
