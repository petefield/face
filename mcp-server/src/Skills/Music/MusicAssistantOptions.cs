namespace Blinky.McpServer.Music;

/// <summary>Options bound from the "Music:MusicAssistant" configuration section.</summary>
public sealed class MusicAssistantOptions
{
    public const string SectionName = "Skills:musicAssistant";
    public const string HttpClientName = "MusicAssistant";

    /// <summary>Base URL of the Music Assistant server, e.g. "http://192.168.1.69:8095".</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Access token sent as a bearer token. Create one under the user profile in Music Assistant.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Id of the player queue to control, usually the same as the player id.</summary>
    public string QueueId { get; set; } = string.Empty;
}
