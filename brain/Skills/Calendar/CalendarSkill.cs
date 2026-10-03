using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Brain.Skills;

/// <summary>Reads upcoming events from the user's Google Calendar (read-only).</summary>
public sealed class CalendarSkill : ISkill
{
    private const int MaxDays = 31;

    private readonly HttpClient _httpClient;
    private readonly GoogleAccessTokenProvider _tokens;
    private readonly GoogleCalendarOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CalendarSkill> _logger;

    public CalendarSkill(
        IHttpClientFactory httpClientFactory,
        GoogleAccessTokenProvider tokens,
        IOptions<GoogleCalendarOptions> options,
        TimeProvider timeProvider,
        ILogger<CalendarSkill> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _tokens = tokens;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string Name => "read_calendar";

    public string Description => "Lists events from the user's Google Calendar. Use this when the user asks what is on their calendar, schedule, or agenda.";

    public BinaryData ParameterSchema => SkillSchema.Create<CalendarArguments>();

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryParse<CalendarArguments>(out var parsed))
        {
            return "Failed: invalid arguments.";
        }

        if (!_tokens.IsConfigured)
        {
            return "Failed: Google Calendar is not configured.";
        }

        var today = _timeProvider.GetLocalNow();
        var start = today.Date;
        if (!string.IsNullOrWhiteSpace(parsed.StartDate))
        {
            if (!DateTime.TryParseExact(parsed.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
            {
                return "Failed: startDate must be formatted as yyyy-MM-dd.";
            }
        }

        var days = Math.Clamp(parsed.Days ?? 1, 1, MaxDays);
        var offset = _timeProvider.LocalTimeZone.GetUtcOffset(start);
        var timeMin = new DateTimeOffset(start, offset);
        var timeMax = timeMin.AddDays(days);

        try
        {
            var query = $"singleEvents=true&orderBy=startTime&maxResults=50" +
                        $"&timeMin={Uri.EscapeDataString(timeMin.ToString("O"))}&timeMax={Uri.EscapeDataString(timeMax.ToString("O"))}";
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(_options.CalendarId)}/events?{query}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokens.GetTokenAsync(cancellationToken));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Calendar returned {StatusCode}.", (int)response.StatusCode);
                return "Failed to read the calendar.";
            }

            var events = await response.Content.ReadFromJsonAsync<EventList>(cancellationToken);
            return Format(events?.Items ?? [], start, days);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Failed to read Google Calendar.");
            return "Failed to read the calendar.";
        }
    }

    private static string Format(IReadOnlyList<CalendarEvent> events, DateTime start, int days)
    {
        var range = days == 1 ? start.ToString("dddd d MMMM") : $"{start:d MMMM} for {days} days";
        if (events.Count == 0)
        {
            return $"No events on {range}.";
        }

        var sb = new StringBuilder($"Events for {range}:\n");
        foreach (var e in events)
        {
            var title = string.IsNullOrWhiteSpace(e.Summary) ? "(no title)" : e.Summary;
            var when = e.Start?.DateTime is { } begin
                ? $"{begin:ddd d MMM HH:mm}" + (e.End?.DateTime is { } end ? $"-{end:HH:mm}" : "")
                : $"{e.Start?.Date} (all day)";
            sb.Append("- ").Append(when).Append(": ").Append(title);
            if (!string.IsNullOrWhiteSpace(e.Location))
            {
                sb.Append(" @ ").Append(e.Location);
            }

            sb.Append('\n');
        }

        return sb.ToString().TrimEnd();
    }

    private sealed record EventList([property: JsonPropertyName("items")] List<CalendarEvent>? Items);

    private sealed record CalendarEvent(
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("location")] string? Location,
        [property: JsonPropertyName("start")] EventTime? Start,
        [property: JsonPropertyName("end")] EventTime? End);

    private sealed record EventTime(
        [property: JsonPropertyName("dateTime")] DateTimeOffset? DateTime,
        [property: JsonPropertyName("date")] string? Date);
}
