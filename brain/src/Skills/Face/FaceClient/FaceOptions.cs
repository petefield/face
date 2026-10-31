namespace Brain;

/// <summary>Options bound from the "Face" configuration section.</summary>
public class FaceOptions
{
    public const string SectionName = "Face";

    /// <summary>Base URL of the face app, e.g. "http://127.0.0.1:5000".</summary>
    public string BaseUrl { get; set; } = string.Empty;
}
