namespace Blinky.McpServer.Music;

/// <summary>Deliberately small music API. Swap the implementation to drive a real player.</summary>
public interface IMusicService
{
    Task<IReadOnlyList<Track>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>Plays the track with the given id, or resumes the paused track when the id is null.</summary>
    Task<PlaybackStatus> PlayAsync(string? trackId = null, CancellationToken cancellationToken = default);

    Task<PlaybackStatus> PauseAsync(CancellationToken cancellationToken = default);

    Task<PlaybackStatus> StopAsync(CancellationToken cancellationToken = default);

    Task<PlaybackStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}
