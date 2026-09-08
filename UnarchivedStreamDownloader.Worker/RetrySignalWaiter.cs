namespace UnarchivedStreamDownloader.Worker;

using UnarchivedStreamDownloader.Core.Infrastructure;
using UnarchivedStreamDownloader.Core.Utilities.Extensions;

public class RetrySignalWaiter : IRetrySignalWaiter
{
    public async Task WaitAsync(TimeSpan timeout, TimeProvider timeProvider)
    {
        // スリープ中は Task.WaitAsync のタイムアウト時間が経過しないため、
        // スリープ復帰(Resume)も監視し、呼び出し元でタイムアウト時間を再計算させる
        using var resumeSignal = new SystemResumeSignal();
        using var cancelSignal = new ConsoleCancelKeyPressSignal();

        // Resume発火直後はまだネットワークアクセスできないため、追加で10秒待機する
        var resumeWaitTask = resumeSignal.WaitAsync()
            .ContinueWith(
                _ => Task.Delay(TimeSpan.FromSeconds(10)),
                TaskContinuationOptions.OnlyOnRanToCompletion)
            .Unwrap();
        var cancelWaitTask = cancelSignal.WaitAsync();

        await Task.WhenAny(resumeWaitTask, cancelWaitTask)
            .WaitAsync(timeout, timeProvider)
            .SuppressThrowing();
    }
}
