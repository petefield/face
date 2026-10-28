using System.Net.Http.Json;

namespace Brain;

/// <summary>Sends text to the speech service. Failures are logged and never thrown.</summary>
public class SpeechService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<SpeechService> logger)
{
    public const string HttpClientName = "Speech";

    private const int Speed = 180;

    private readonly string _speechUrl = configuration["Speech:Url"] ?? "http://localhost:5060/speak";

    public async Task SpeakAsync(string? message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            using var http = httpClientFactory.CreateClient(HttpClientName);
            using var response = await http.PostAsJsonAsync(_speechUrl, new { text = message, speed = Speed }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Speech service returned {StatusCode} {Reason}: {Body}", (int)response.StatusCode, response.ReasonPhrase, body);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to speak through {Url}.", _speechUrl);
        }
    }
}
