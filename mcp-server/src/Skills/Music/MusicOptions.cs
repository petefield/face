namespace Blinky.McpServer.Music;

/// <summary>Options bound from the "Music" configuration section.</summary>
public sealed class MusicOptions
{
    public const string SectionName = "Music";

    public List<Track> Library { get; set; } = [];
}
