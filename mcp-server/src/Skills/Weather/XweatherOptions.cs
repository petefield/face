namespace Brain.Skills;

public sealed class XweatherOptions
{
    public const string SectionName = "Skills:xweather";
    public const string HttpClientName = "Xweather";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string DefaultLocation { get; set; } = string.Empty;
}
