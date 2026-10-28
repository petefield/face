using Microsoft.Extensions.Options;

namespace Blinky.McpServer.Music;

/// <summary>Searches the configured library and tracks play/pause/stop state in memory.</summary>
public sealed class InMemoryMusicService : IMusicService
{
    private readonly IReadOnlyList<Track> _library;
    private readonly ILogger<InMemoryMusicService> _logger;
    private readonly object _gate = new();
    private PlaybackState _state = PlaybackState.Stopped;
    private Track? _current;

    public InMemoryMusicService(IOptions<MusicOptions> options, ILogger<InMemoryMusicService> logger)
    {
        _library = options.Value.Library;
        _logger = logger;
    }

    public Task<IReadOnlyList<Track>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<Track>>([]);
        }

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IReadOnlyList<Track> results = _library
            .Where(track => terms.All(term =>
                track.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || track.Artist.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (track.Album?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)))
            .Take(Math.Clamp(limit, 1, 50))
            .ToList();
        return Task.FromResult(results);
    }

    public Task<PlaybackStatus> PlayAsync(string? trackId = null, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (trackId is null)
            {
                if (_current is not null)
                {
                    _state = PlaybackState.Playing;
                }
            }
            else if (_library.FirstOrDefault(t => string.Equals(t.Id, trackId, StringComparison.OrdinalIgnoreCase)) is { } track)
            {
                _current = track;
                _state = PlaybackState.Playing;
            }
            else
            {
                throw new KeyNotFoundException($"No track with id '{trackId}'.");
            }

            _logger.LogInformation("Music {State}: {Track}", _state, _current?.Title);
            return Task.FromResult(Snapshot());
        }
    }

    public Task<PlaybackStatus> PauseAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_state == PlaybackState.Playing)
            {
                _state = PlaybackState.Paused;
            }

            return Task.FromResult(Snapshot());
        }
    }

    public Task<PlaybackStatus> StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _state = PlaybackState.Stopped;
            _current = null;
            return Task.FromResult(Snapshot());
        }
    }

    public Task<PlaybackStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(Snapshot());
        }
    }

    private PlaybackStatus Snapshot() => new(_state, _current);
}
