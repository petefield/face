namespace Blinky.McpServer.Music;

public sealed record Track(string Id, string Title, string Artist, string? Album = null, string? Url = null);

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused,
}

public sealed record PlaybackStatus(PlaybackState State, Track? Track);
