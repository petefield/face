using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Brain.Skills;

public sealed class WeatherSkill : ISkill
{
    private readonly HttpClient _httpClient;
    private readonly XweatherOptions _options;
    private readonly ILogger<WeatherSkill> _logger;

    public WeatherSkill(
        IHttpClientFactory httpClientFactory,
        IOptions<XweatherOptions> options,
        ILogger<WeatherSkill> logger)
    {
        _httpClient = httpClientFactory.CreateClient(XweatherOptions.HttpClientName);
        _options = options.Value;
        _logger = logger;
    }

    public string Name => "get_weather";

    public string Description =>
        "Gets current weather or daily forecasts from Xweather for a specified location or the configured default. " +
        "Use this for weather, temperature, wind, or rain forecasts. Do not invent a location or weather data. " +
        "Forecast availability depends on the Xweather subscription; historical weather is not supported.";

    public BinaryData ParameterSchema => SkillSchema.Create<WeatherArguments>();

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryParse<WeatherArguments>(out var parsed))
        {
            return Fail("invalid arguments.");
        }

        var location = string.IsNullOrWhiteSpace(parsed.Location) ? _options.DefaultLocation : parsed.Location;
        if (string.IsNullOrWhiteSpace(location))
        {
            return Fail("specify a location or configure Skills:xweather:DefaultLocation.");
        }

        var report = parsed.Report ?? WeatherReportType.Current;
        var units = parsed.Units ?? WeatherUnits.Metric;
        var days = parsed.Days ?? 3;
        if (report == WeatherReportType.Current && (parsed.Days is not null || parsed.StartDate is not null))
        {
            return Fail("days and startDate are only valid for forecasts.");
        }

        if (report == WeatherReportType.Forecast && days is < 1 or > 7)
        {
            return Fail("days must be between 1 and 7.");
        }

        if (parsed.StartDate is not null &&
            !DateOnly.TryParseExact(parsed.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return Fail("startDate must be formatted as yyyy-MM-dd.");
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            return Fail("Xweather is not configured. Set Skills:xweather:ClientId and ClientSecret.");
        }

        var endpoint = report == WeatherReportType.Current ? "observations" : "forecasts";
        var query = $"client_id={Uri.EscapeDataString(_options.ClientId)}&client_secret={Uri.EscapeDataString(_options.ClientSecret)}";
        if (report == WeatherReportType.Forecast)
        {
            query += $"&filter=day&limit={days}";
            if (parsed.StartDate is not null)
            {
                query += $"&from={Uri.EscapeDataString(parsed.StartDate)}";
            }
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{endpoint}/{Uri.EscapeDataString(location.Trim())}?{query}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Xweather returned HTTP {StatusCode}.", (int)response.StatusCode);
                return "Failed: Xweather could not retrieve the weather.";
            }

            var payload = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken);
            if (payload is null || !payload.Success)
            {
                return Fail("Xweather rejected the request. Check the location, credentials, and subscription.");
            }

            if (payload.Error.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                return Fail("Xweather returned a warning or error; reliable weather data is unavailable.");
            }

            // Observations can be an object or array; forecasts always return an array.
            var data = payload.Response;
            if (data.ValueKind == JsonValueKind.Array)
            {
                if (data.GetArrayLength() == 0)
                {
                    return Fail("Xweather returned no weather data for this location.");
                }

                data = data[0];
            }

            if (data.ValueKind != JsonValueKind.Object)
            {
                return Fail("Xweather returned an invalid weather response.");
            }

            var weather = data.Deserialize<WeatherData>(SkillArguments.SerializerOptions);
            var imperial = units == WeatherUnits.Imperial;
            var temperatureUnit = imperial ? "F" : "C";
            var windSpeedUnit = imperial ? "mph" : "km/h";
            if (report == WeatherReportType.Current)
            {
                if (weather?.Ob is not { } observation ||
                    (string.IsNullOrWhiteSpace(observation.Weather) && observation.TempC is null && observation.TempF is null))
                {
                    return Fail("Xweather returned no current conditions for this location.");
                }

                return JsonSerializer.Serialize(new
                {
                    source = "Xweather",
                    location,
                    station = weather.Place,
                    report,
                    temperatureUnit,
                    windSpeedUnit,
                    observation.DateTimeISO,
                    observation.Weather,
                    temperature = imperial ? observation.TempF : observation.TempC,
                    feelsLike = imperial ? observation.FeelslikeF : observation.FeelslikeC,
                    observation.Humidity,
                    windSpeed = imperial ? observation.WindSpeedMPH : observation.WindSpeedKPH,
                    observation.WindDir,
                }, SkillArguments.SerializerOptions);
            }

            if (weather?.Periods is not { Count: > 0 } periods ||
                periods.Any(period => period.DateTimeISO is null ||
                    (string.IsNullOrWhiteSpace(period.Weather) && period.MaxTempC is null && period.MaxTempF is null)))
            {
                return Fail("Xweather returned no usable daily forecast for this location.");
            }

            return JsonSerializer.Serialize(new
            {
                source = "Xweather",
                location,
                place = weather.Place,
                report,
                temperatureUnit,
                windSpeedUnit,
                periods = periods.Take(days).Select(period => new
                {
                    period.DateTimeISO,
                    period.Weather,
                    high = imperial ? period.MaxTempF : period.MaxTempC,
                    low = imperial ? period.MinTempF : period.MinTempC,
                    precipitationChancePercent = period.Pop,
                    windSpeed = imperial ? period.WindSpeedMPH : period.WindSpeedKPH,
                    period.WindDir,
                }),
            }, SkillArguments.SerializerOptions);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Fail("the Xweather request timed out.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException)
        {
            // Request URIs and exception messages can contain credentials.
            _logger.LogWarning("Xweather request failed ({ErrorType}).", ex.GetType().Name);
            return "Failed: unable to read weather from Xweather.";
        }
    }

    private string Fail(string message)
    {
        _logger.LogWarning("Weather skill failed: {Reason}", message);
        return $"Failed: {message}";
    }

    private sealed record ApiResponse(bool Success, JsonElement Error, JsonElement Response);

    private sealed record WeatherData(WeatherPlace? Place, Observation? Ob, List<ForecastPeriod>? Periods);

    private sealed record WeatherPlace(string? Name, string? State, string? Country);

    private sealed record Observation(
        DateTimeOffset? DateTimeISO, string? Weather,
        double? TempC, double? TempF, double? FeelslikeC, double? FeelslikeF,
        double? Humidity, double? WindSpeedKPH, double? WindSpeedMPH, string? WindDir);

    private sealed record ForecastPeriod(
        DateTimeOffset? DateTimeISO, string? Weather,
        double? MaxTempC, double? MaxTempF, double? MinTempC, double? MinTempF,
        double? Pop, double? WindSpeedKPH, double? WindSpeedMPH, string? WindDir);
}
