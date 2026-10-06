namespace Brain.Skills;

/// <summary>Options bound from the "Skills:googleCalendar" configuration section.</summary>
public class GoogleCalendarOptions
{
    public const string SectionName = "Skills:googleCalendar";

    /// <summary>Path to the service account's JSON key file.</summary>
    public string ServiceAccountKeyFile { get; set; } = string.Empty;

    /// <summary>ID of the calendar to read (usually the owner's email). It must be shared with the service account.</summary>
    public string CalendarId { get; set; } = string.Empty;
}
