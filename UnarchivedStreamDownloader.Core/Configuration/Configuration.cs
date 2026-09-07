namespace UnarchivedStreamDownloader.Core.Configuration;

using Microsoft.Extensions.Configuration;

public static class Configuration
{
    public static TValue Load<TValue>(string filePath)
    {
        var value = new ConfigurationBuilder()
            .AddJsonFile(filePath)
            .Build()
            .Get<TValue>();

        return value ?? throw new InvalidOperationException($"'{filePath}' is not configured.");
    }
}
