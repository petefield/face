using System.ComponentModel;

namespace Brain.Skills;

public enum WeatherReportType
{
    Current,
    Forecast,
}

public enum WeatherUnits
{
    Metric,
    Imperial,
}

public sealed record WeatherArguments(
    [property: Description("City with state or country (london,gb), postal code, or latitude,longitude. Omit to use the configured default; ask the user if no default is configured.")] string? Location,
    [property: Description("current for current conditions, forecast for daily forecasts. Omit for current.")] WeatherReportType? Report,
    [property: Description("Forecast days to return, from 1 to 7. Omit for 3. Only valid for forecast.")] int? Days,
    [property: Description("First forecast date as yyyy-MM-dd, in the requested location's local time. Omit to start today. Only valid for forecast; historical weather is not supported.")] string? StartDate,
    [property: Description("metric uses Celsius and km/h; imperial uses Fahrenheit and mph. Omit for metric.")] WeatherUnits? Units);
