using System.ComponentModel;

namespace Brain.Skills;

/// <summary>Arguments for reading the calendar.</summary>
public record CalendarArguments(
    [property: Description("First day to list, as yyyy-MM-dd in local time. Omit for today.")] string? StartDate,
    [property: Description("How many days to list, starting from the start date. Omit for 1.")] int? Days);
