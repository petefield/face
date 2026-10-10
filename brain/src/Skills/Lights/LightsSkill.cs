using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Brain.Skills;

/// <summary>Turns the robot's lights on or off through the Home Assistant REST API.</summary>
public sealed class LightsSkill : ISkill
{
    private readonly HttpClient _httpClient;
    
    private readonly ILogger<LightsSkill> _logger;

    public LightsSkill(IHttpClientFactory httpClientFactory, IOptions<HomeAssistantOptions> options, ILogger<LightsSkill> logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        if (!string.IsNullOrWhiteSpace(options.Value.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute);
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.AccessToken);
    }

    public string Name => "control_lights";

    public string Description => "Turns the home lights on or off. Use this when the user asks for the lights to be switched.";

    public BinaryData ParameterSchema => SkillSchema.Create<LightsArguments>();

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryParse<LightsArguments>(out var parsed))
        {
            return "Failed: 'device' and 'state' are required.";
        }

        if (_httpClient.BaseAddress is null)
        {
            return "Failed: Home Assistant is not configured.";
        }

        var service = parsed.State == LightState.On ? "turn_on" : "turn_off";
        var failed = new List<LightDevice>();

        foreach (var device in parsed.Devices)
        {
            var deviceId = typeof(LightDevice).GetField(device.ToString())!.GetCustomAttribute<DeviceIDAttribute>()!.Id;
            var entityId = $"light.{deviceId}";
            _logger.LogInformation("Setting {Device} lights {State}.", device, parsed.State);

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(
                    $"api/services/light/{service}", new { entity_id = entityId }, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Home Assistant returned {StatusCode} for {EntityId}.", (int)response.StatusCode, entityId);
                    failed.Add(device);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Failed to reach Home Assistant for {EntityId}.", entityId);
                failed.Add(device);
            }
        }

        if (failed.Count > 0)
        {
            return $"Failed to switch the {string.Join(", ", failed)} lights.".ToLowerInvariant();
        }

        return $"The {string.Join(", ", parsed.Devices)} lights are now {parsed.State}.".ToLowerInvariant();
    }
}
