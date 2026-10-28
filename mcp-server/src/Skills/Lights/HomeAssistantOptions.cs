namespace Brain.Skills;

/// <summary>Options bound from the "Skills:homeAssistant" configuration section.</summary>
public class HomeAssistantOptions
{
    public const string SectionName = "Skills:homeAssistant";

    /// <summary>Base URL of Home Assistant, e.g. "http://pippin.local:8123/".</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Long-lived access token used as a bearer token.</summary>
    public string AccessToken { get; set; } = string.Empty;
}
