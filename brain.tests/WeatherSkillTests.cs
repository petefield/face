using System.Net;
using System.Text;
using System.Text.Json;
using Brain.Skills;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brain.Tests;

public sealed class WeatherSkillTests
{
    private const string Observation = """
        {"place":{"name":"london","country":"gb"},"ob":{
          "dateTimeISO":"2026-10-06T08:00:00+01:00","weather":"Cloudy",
          "tempC":12,"tempF":53.6,"feelslikeC":10,"feelslikeF":50,
          "humidity":80,"windSpeedKPH":16,"windSpeedMPH":10,"windDir":"SW"}}
        """;

    [Theory]
    [InlineData("metric", 12, 16, "C", "km/h")]
    [InlineData("imperial", 53.6, 10, "F", "mph")]
    public async Task CurrentWeatherUsesRequestedUnits(
        string units, double temperature, double windSpeed, string temperatureUnit, string windSpeedUnit)
    {
        using var harness = new Harness($$"""{"success":true,"error":null,"response":{{Observation}}}""");
        var result = await harness.Run($$"""{"location":"london,gb","units":"{{units}}"}""");
        using var json = JsonDocument.Parse(result);
        Assert.Equal(temperature, json.RootElement.GetProperty("temperature").GetDouble());
        Assert.Equal(windSpeed, json.RootElement.GetProperty("windSpeed").GetDouble());
        Assert.Equal(temperatureUnit, json.RootElement.GetProperty("temperatureUnit").GetString());
        Assert.Equal(windSpeedUnit, json.RootElement.GetProperty("windSpeedUnit").GetString());
        Assert.Equal("current", json.RootElement.GetProperty("report").GetString());
        Assert.Equal("2026-10-06T08:00:00+01:00", json.RootElement.GetProperty("dateTimeISO").GetString());
        Assert.Equal("Xweather", harness.Factory.RequestedName);
        Assert.StartsWith("/observations/", harness.Handler.RequestUri!.AbsolutePath);
        Assert.Contains("client_id=test%20id", harness.Handler.RequestUri.Query);
        Assert.Contains("client_secret=test%26secret", harness.Handler.RequestUri.Query);
        Assert.DoesNotContain("test&secret", result);
    }

    [Fact]
    public async Task CurrentWeatherAcceptsArrayAndUsesDefaultLocation()
    {
        using var harness = new Harness($$"""{"success":true,"response":[{{Observation}}]}""");
        Assert.DoesNotContain("Failed:", await harness.Run("{}"));
        Assert.Contains("london", harness.Handler.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task LocationCannotInjectQueryParameters()
    {
        using var harness = new Harness($$"""{"success":true,"response":{{Observation}}}""");
        await harness.Run("""{"location":"51.5,-0.1?client_secret=evil&filter=day"}""");
        Assert.Contains("%3Fclient_secret%3Devil%26filter%3Dday", harness.Handler.RequestUri!.AbsolutePath);
        Assert.DoesNotContain("evil", harness.Handler.RequestUri.Query);
    }

    [Fact]
    public async Task MissingFieldsRemainNullRatherThanZero()
    {
        using var harness = new Harness("""
            {"success":true,"response":{"ob":{"weather":"Cloudy"}}}
            """);
        using var json = JsonDocument.Parse(await harness.Run("{}"));
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("temperature").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("windSpeed").ValueKind);
    }

    [Theory]
    [InlineData("metric", 20, 10, 16)]
    [InlineData("imperial", 68, 50, 10)]
    public async Task ForecastUsesDailyPeriodsAndStartDate(string units, double high, double low, double wind)
    {
        using var harness = new Harness("""
            {"success":true,"error":null,"response":[{"place":{"name":"london"},"periods":[
              {"dateTimeISO":"2026-10-07T07:00:00+01:00","weather":"Rain",
               "maxTempC":20,"maxTempF":68,"minTempC":10,"minTempF":50,
               "pop":75,"windSpeedKPH":16,"windSpeedMPH":10},
              {"dateTimeISO":"2026-10-08T07:00:00+01:00","weather":"Cloudy"},
              {"dateTimeISO":"2026-10-09T07:00:00+01:00","weather":"Sunny"}]}]}
            """);
        using var json = JsonDocument.Parse(await harness.Run(
            $$"""{"report":"forecast","days":2,"startDate":"2026-10-07","units":"{{units}}"}"""));
        var periods = json.RootElement.GetProperty("periods");
        Assert.Equal(2, periods.GetArrayLength());
        Assert.Equal(high, periods[0].GetProperty("high").GetDouble());
        Assert.Equal(low, periods[0].GetProperty("low").GetDouble());
        Assert.Equal(wind, periods[0].GetProperty("windSpeed").GetDouble());
        Assert.Equal(75, periods[0].GetProperty("precipitationChancePercent").GetDouble());
        Assert.StartsWith("/forecasts/", harness.Handler.RequestUri!.AbsolutePath);
        Assert.Contains("filter=day&limit=2&from=2026-10-07", harness.Handler.RequestUri.Query);
    }

    [Fact]
    public async Task ForecastDefaultsToThreeDays()
    {
        using var harness = new Harness("""{"success":true,"response":[]}""");
        await harness.Run("""{"report":"forecast"}""");
        Assert.Contains("filter=day&limit=3", harness.Handler.RequestUri!.Query);
    }

    [Theory]
    [InlineData("""{"report":"forecast","days":0}""")]
    [InlineData("""{"report":"forecast","days":8}""")]
    [InlineData("""{"days":2}""")]
    [InlineData("""{"startDate":"2026-10-07"}""")]
    [InlineData("""{"report":"forecast","startDate":"2026-02-30"}""")]
    [InlineData("""{"report":"forecast","startDate":""}""")]
    [InlineData("""{"report":"unknown"}""")]
    [InlineData("""{"report":1}""")]
    [InlineData("""{"units":"kelvin"}""")]
    [InlineData("""{"days":"three"}""")]
    [InlineData("null")]
    [InlineData("[]")]
    public async Task InvalidArgumentsFailWithoutCallingProvider(string arguments)
    {
        using var harness = new Harness("{}");
        Assert.StartsWith("Failed:", await harness.Run(arguments));
        Assert.Null(harness.Handler.RequestUri);
        Assert.NotEmpty(harness.Logger.Messages);
    }

    [Theory]
    [InlineData("", "secret", "london,gb")]
    [InlineData("id", "", "london,gb")]
    [InlineData("id", "secret", "")]
    public async Task MissingConfigurationFailsWithoutCallingProvider(string id, string secret, string location)
    {
        using var harness = new Harness("{}", new XweatherOptions
        {
            ClientId = id, ClientSecret = secret, DefaultLocation = location,
        });
        Assert.StartsWith("Failed:", await harness.Run("{}"));
        Assert.Null(harness.Handler.RequestUri);
    }

    [Theory]
    [InlineData("""{"success":false,"error":{"code":"invalid_location"},"response":null}""", "{}")]
    [InlineData("""{"success":true,"error":{"code":"warn_no_data"},"response":{}}""", "{}")]
    [InlineData("""{"success":true,"response":[]}""", "{}")]
    [InlineData("""{"success":true,"response":null}""", "{}")]
    [InlineData("""{"success":true,"response":{"ob":{}}}""", "{}")]
    [InlineData("""{"success":true,"response":[{"periods":[]}]}""", """{"report":"forecast"}""")]
    [InlineData("""{"success":true,"response":[{"periods":[{}]}]}""", """{"report":"forecast"}""")]
    [InlineData("""{"success":true,"response":{"ob":{"tempC":"bad"}}}""", "{}")]
    [InlineData("not json", "{}")]
    public async Task ProviderFailuresDoNotInventWeather(string payload, string arguments)
    {
        using var harness = new Harness(payload);
        Assert.StartsWith("Failed:", await harness.Run(arguments));
        Assert.NotEmpty(harness.Logger.Messages);
    }

    [Fact]
    public async Task HttpErrorIsExplicitAndDoesNotExposeResponseBody()
    {
        using var harness = new Harness("test&secret");
        harness.Handler.StatusCode = HttpStatusCode.Unauthorized;
        var result = await harness.Run("{}");
        Assert.StartsWith("Failed:", result);
        Assert.DoesNotContain("test&secret", result);
        Assert.DoesNotContain("test&secret", string.Join("", harness.Logger.Messages));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransportFailuresAndTimeoutsAreSafe(bool timeout)
    {
        using var harness = new Harness("{}");
        harness.Handler.Exception = timeout
            ? new TaskCanceledException("test&secret")
            : new HttpRequestException("test&secret");
        var result = await harness.Run("{}");
        Assert.StartsWith("Failed:", result);
        Assert.DoesNotContain("test&secret", result);
        Assert.DoesNotContain("test&secret", string.Join("", harness.Logger.Messages));
    }

    [Fact]
    public async Task CallerCancellationPropagates()
    {
        using var harness = new Harness("{}");
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.Run("{}", cts.Token));
    }

    [Fact]
    public void SkillIsAutomaticallyRegisteredAndSchemaUsesEnumStrings()
    {
        var services = new ServiceCollection().AddSkills();
        Assert.Contains(services, service => service.ServiceType == typeof(ISkill) &&
            service.ImplementationType == typeof(WeatherSkill));
        using var harness = new Harness("{}");
        using var schema = JsonDocument.Parse(harness.Skill.ParameterSchema.ToString());
        var properties = schema.RootElement.GetProperty("properties");
        Assert.Contains("\"forecast\"", properties.GetProperty("report").GetRawText());
        Assert.Contains("\"imperial\"", properties.GetProperty("units").GetRawText());
        Assert.True(properties.TryGetProperty("startDate", out _));
        Assert.False(schema.RootElement.GetProperty("additionalProperties").GetBoolean());
    }

    private sealed class Harness : IDisposable
    {
        private readonly HttpClient _client;
        public StubHandler Handler { get; }
        public StubFactory Factory { get; }
        public RecordingLogger Logger { get; } = new();
        public WeatherSkill Skill { get; }

        public Harness(string payload, XweatherOptions? options = null)
        {
            Handler = new StubHandler(payload);
            _client = new HttpClient(Handler) { BaseAddress = new Uri("https://data.api.xweather.com/") };
            Factory = new StubFactory(_client);
            Skill = new WeatherSkill(Factory, Options.Create(options ?? new XweatherOptions
            {
                ClientId = "test id", ClientSecret = "test&secret", DefaultLocation = "london,gb",
            }), Logger);
        }

        public async Task<string> Run(string arguments, CancellationToken cancellationToken = default)
        {
            using var json = JsonDocument.Parse(arguments);
            return await Skill.ExecuteAsync(json.RootElement, cancellationToken);
        }

        public void Dispose() => _client.Dispose();
    }

    private sealed class StubFactory(HttpClient client) : IHttpClientFactory
    {
        public string? RequestedName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            RequestedName = name;
            return client;
        }
    }

    private sealed class StubHandler(string payload) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public Exception? Exception { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestUri = request.RequestUri;
            if (Exception is not null)
            {
                return Task.FromException<HttpResponseMessage>(Exception);
            }

            return Task.FromResult(new HttpResponseMessage(StatusCode)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class RecordingLogger : ILogger<WeatherSkill>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
