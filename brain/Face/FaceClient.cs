using Microsoft.Extensions.Options;

namespace Brain;

/// <summary>Notifies the face app of the robot's current emotional state via its HTTP API.</summary>
public class FaceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FaceClient> _logger;

    public FaceClient(HttpClient httpClient, IOptions<FaceOptions> options, ILogger<FaceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(options.Value.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute);
        }
    }

    /// <summary>
    /// Posts the given emotion to the face app's "/emotion/{emotion}" endpoint. Failures are logged
    /// but not thrown, so an unreachable face app never fails the caller's prompt request.
    /// </summary>
    public async Task SetEmotionAsync(EmotionState emotion, CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress is null)
        {
            _logger.LogWarning("Face:BaseUrl is not configured; skipping emotion update.");
            return;
        }

        var emotionValue = emotion.ToString().ToLowerInvariant();

        try
        {
            using var response = await _httpClient.PostAsync($"/emotion/{emotionValue}", content: null, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Face app returned {StatusCode} when setting emotion to {Emotion}.",
                    (int)response.StatusCode,
                    emotionValue);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to notify face app of emotion {Emotion}.", emotionValue);
        }
    }

    /// <summary>
    /// Posts a gaze direction to the face app's "/look/{x}/{y}" endpoint.
    /// Returns false if the face app is unconfigured, unreachable, or rejects the request.
    /// </summary>
    public async Task<bool> LookAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        if (_httpClient.BaseAddress is null)
        {
            _logger.LogWarning("Face:BaseUrl is not configured; skipping gaze update.");
            return false;
        }

        try
        {
            using var response = await _httpClient.PostAsync($"/look/{x}/{y}", content: null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning(
                "Face app returned {StatusCode} when setting gaze to {X},{Y}.",
                (int)response.StatusCode,
                x,
                y);
            return false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to set gaze to {X},{Y}.", x, y);
            return false;
        }
    }
}
