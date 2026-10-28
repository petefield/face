using ModelContextProtocol;
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Blinky.McpServer.Music;

[McpServerToolType]
public sealed class MusicTools(IMusicService music)
{
    [McpServerTool(Name = "music_search", ReadOnly = true), Description("Searches the music library by title, artist or album. Returns tracks with ids to pass to music_play.")]
    public Task<IReadOnlyList<Track>> Search([Description("Words to look for in the title, artist or album.")] string query, CancellationToken cancellationToken) =>
        music.SearchAsync(query, cancellationToken: cancellationToken);

    [McpServerTool(Name = "music_play"), Description("Plays a track by id (from music_search), or resumes the paused track if no id is given.")]
    public async Task<PlaybackStatus> Play([Description("Track id from music_search. Omit to resume.")] string? trackId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            return await music.PlayAsync(trackId, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "music_pause"), Description("Pauses the current track.")]
    public Task<PlaybackStatus> Pause(CancellationToken cancellationToken) => music.PauseAsync(cancellationToken);

    [McpServerTool(Name = "music_stop"), Description("Stops playback and clears the current track.")]
    public Task<PlaybackStatus> Stop(CancellationToken cancellationToken) => music.StopAsync(cancellationToken);

    [McpServerTool(Name = "music_status", ReadOnly = true), Description("Gets the current playback state and track.")]
    public Task<PlaybackStatus> Status(CancellationToken cancellationToken) => music.GetStatusAsync(cancellationToken);
}
