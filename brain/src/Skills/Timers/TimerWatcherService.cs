using System.Globalization;

namespace Brain.Skills;

/// <summary>
/// Every 10 seconds, removes due (and malformed) rows from the timers file and publishes a
/// <see cref="TimerFired"/> event for each due timer.
/// </summary>
public sealed class TimerWatcherService(TimeProvider timeProvider, TimersFile timersFile, TimerEvents timerEvents, ILogger<TimerWatcherService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await ProcessDueTimersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process the timers file.");
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private async Task ProcessDueTimersAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // Due and unparseable rows are both removed; unparseable ones would otherwise be retried forever.
        var removed = await timersFile.RemoveWhereAsync(
            row => !TryParse(row, out var fireAt) || fireAt <= now,
            cancellationToken);

        foreach (var row in removed)
        {
            if (!TryParse(row, out var fireAt))
            {
                logger.LogWarning("Discarded malformed timer row '{Row}'.", row);
                continue;
            }

            logger.LogInformation("Timer due {FireAt}.", fireAt);
            await timerEvents.PublishAsync(new TimerFired(fireAt), cancellationToken);
        }
    }

    private static bool TryParse(string row, out DateTimeOffset fireAt) =>
        DateTimeOffset.TryParse(row, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out fireAt);

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
