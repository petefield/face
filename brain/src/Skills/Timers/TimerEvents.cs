using System.Threading.Channels;

namespace Brain.Skills;

/// <summary>Raised when a timer reaches its fire time.</summary>
public record TimerFired(DateTimeOffset FireAt);

/// <summary>Singleton channel carrying <see cref="TimerFired"/> events from the watcher to a consumer.</summary>
public class TimerEvents
{
    private readonly Channel<TimerFired> _channel = Channel.CreateUnbounded<TimerFired>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask PublishAsync(TimerFired timerFired, CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(timerFired, cancellationToken);

    public IAsyncEnumerable<TimerFired> ReadAllAsync(CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
