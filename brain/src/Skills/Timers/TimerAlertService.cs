namespace Brain.Skills;

/// <summary>Consumes <see cref="TimerFired"/> events: sets the face and announces the timer through the speech service.</summary>
public sealed class TimerAlertService(
    TimerEvents timerEvents,
    SpeechService speech,
    ILogger<TimerAlertService> logger) : BackgroundService
{
    private const string Message = "Your timer is done.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var fired in timerEvents.ReadAllAsync(stoppingToken))
            {
                logger.LogInformation("Announcing timer that was due {FireAt}.", fired.FireAt);
                await AnnounceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    // A failure must not stop the consumer, or later timers would never be announced.
    private async Task AnnounceAsync(CancellationToken cancellationToken)
    {
        await speech.SpeakAsync(Message, cancellationToken);
    }
}
