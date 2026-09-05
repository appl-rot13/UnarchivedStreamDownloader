namespace UnarchivedStreamDownloader.Worker;

using System.Text.Json.Nodes;
using UnarchivedStreamDownloader.Core.Configuration.Models;
using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;

public class YouTubeLiveStartWaiter(
    ILogger logger,
    TimeProvider timeProvider,
    BehaviorSettings behavior,
    IVideoDownloader downloader,
    IConsoleSignalWaiter signalWaiter) : IYouTubeLiveStartWaiter
{
    public async Task<bool> WaitForStartAsync(string videoId)
    {
        const string statusKey = "live_status";
        const string timestampKey = "release_timestamp";

        while (true)
        {
            var videoDetails = await this.GetVideoDetailsAsync(videoId);
            if (!videoDetails.ContainsKey(statusKey) || !videoDetails.ContainsKey(timestampKey))
            {
                throw new InvalidOperationException($"Unexpected object: {videoDetails}");
            }

            var status = videoDetails[statusKey]?.GetValue<string>();
            var timestamp = videoDetails[timestampKey]?.GetValue<int>();
            if (status == null && timestamp == null)
            {
                // 配信が削除された場合
                logger.WriteLine("The video is either private or has been removed.");
                return false;
            }

            if (status != "is_upcoming")
            {
                // 配信が開始した場合
                logger.WriteLine("The video has started.");
                return true;
            }

            var scheduledStartTime = timeProvider.GetLocalNow().DateTime;
            if (timestamp.HasValue)
            {
                scheduledStartTime = DateTimeOffset.FromUnixTimeSeconds(timestamp.Value).LocalDateTime;
                logger.WriteLine($"The video is scheduled to start at {scheduledStartTime}.");
            }

            // Timeline:
            //   |<- timeRemaining ->|<- StartCheckBuffer ->|
            //  -+-------------------+----------------------+-
            //  Now             attemptTime         scheduledStartTime

            var attemptTime = scheduledStartTime.Subtract(behavior.StartCheckBuffer);
            var timeRemaining = (attemptTime - timeProvider.GetLocalNow().DateTime).TruncateToSeconds();
            if (timeRemaining <= TimeSpan.Zero)
            {
                // 配信開始直前の場合
                timeRemaining = behavior.StartCheckInterval;
                attemptTime = timeProvider.GetLocalNow().DateTime.Add(timeRemaining);
            }

            logger.WriteLine($"Wait until {attemptTime} (Time remaining: {timeRemaining}) - Press Ctrl+C to try now.");
            await signalWaiter.WaitForCancelKeyPressAsync(timeRemaining, timeProvider);
        }
    }

    private async Task<JsonObject> GetVideoDetailsAsync(string videoId)
    {
        var jsonString = await downloader.GetVideoDetailsAsync(videoId);
        var jsonObject = JsonNode.Parse(jsonString)?.AsObject();
        if (jsonObject == null)
        {
            throw new InvalidOperationException($"Unexpected output: {jsonString}");
        }

        return jsonObject;
    }
}
