using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Brain.Skills;

/// <summary>Obtains and caches read-only Google Calendar access tokens for a service account.</summary>
public sealed class GoogleAccessTokenProvider
{
    private const string Scope = "https://www.googleapis.com/auth/calendar.readonly";

    private readonly HttpClient _httpClient;
    private readonly GoogleCalendarOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public GoogleAccessTokenProvider(IHttpClientFactory httpClientFactory, IOptions<GoogleCalendarOptions> options, TimeProvider timeProvider)
    {
        _httpClient = httpClientFactory.CreateClient();
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.ServiceAccountKeyFile) && !string.IsNullOrWhiteSpace(_options.CalendarId);

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var now = _timeProvider.GetUtcNow();
            if (_token is not null && now < _expiresAt - TimeSpan.FromMinutes(1))
            {
                return _token;
            }

            var key = JsonSerializer.Deserialize<ServiceAccountKey>(
                await File.ReadAllTextAsync(_options.ServiceAccountKeyFile, cancellationToken))
                ?? throw new InvalidOperationException("Service account key file is empty.");

            var tokenUri = string.IsNullOrWhiteSpace(key.TokenUri) ? "https://oauth2.googleapis.com/token" : key.TokenUri;
            var assertion = CreateAssertion(key, tokenUri, now);

            using var response = await _httpClient.PostAsync(tokenUri, new FormUrlEncodedContent(
            [
                new("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new("assertion", assertion),
            ]), cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Empty token response.");

            _token = token.AccessToken;
            _expiresAt = now.AddSeconds(token.ExpiresIn);
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static string CreateAssertion(ServiceAccountKey key, string audience, DateTimeOffset now)
    {
        var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new
        {
            iss = key.ClientEmail,
            scope = Scope,
            aud = audience,
            iat = now.ToUnixTimeSeconds(),
            exp = now.AddHours(1).ToUnixTimeSeconds(),
        }));

        using var rsa = RSA.Create();
        rsa.ImportFromPem(key.PrivateKey);
        var signature = rsa.SignData(Encoding.ASCII.GetBytes($"{header}.{claims}"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{header}.{claims}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record ServiceAccountKey(
        [property: JsonPropertyName("client_email")] string ClientEmail,
        [property: JsonPropertyName("private_key")] string PrivateKey,
        [property: JsonPropertyName("token_uri")] string? TokenUri);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
