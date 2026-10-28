using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Blinky.McpServer.Music;

/// <summary>Searches and controls playback through the Music Assistant command API (POST /api).</summary>
public sealed class MusicAssistantMusicService : IMusicService
{
    private readonly HttpClient _http;
    private readonly MusicAssistantOptions _options;
    private readonly ILogger<MusicAssistantMusicService> _logger;

    public MusicAssistantMusicService(
        IHttpClientFactory httpClientFactory,
        IOptions<MusicAssistantOptions> options,
        ILogger<MusicAssistantMusicService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _http = httpClientFactory.CreateClient(MusicAssistantOptions.HttpClientName);
    }

    public async Task<IReadOnlyList<Track>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        limit = Math.Clamp(limit, 1, 50);
        var result = await SendAsync("music/search", new JsonObject
        {
            ["search_query"] = query,
            ["media_types"] = new JsonArray("track"),
            ["limit"] = limit,
        }, cancellationToken);

        return result?["tracks"] is JsonArray tracks
            ? tracks.Select(ToTrack).OfType<Track>().Take(limit).ToList()
            : [];
    }

    public async Task<PlaybackStatus> PlayAsync(string? trackId = null, CancellationToken cancellationToken = default)
    {
        var queueId = RequireQueueId();
        if (trackId is null)
        {
            await SendAsync("player_queues/play", new JsonObject { ["queue_id"] = queueId }, cancellationToken);
        }
        else
        {
            await SendAsync("player_queues/play_media", new JsonObject
            {
                ["queue_id"] = queueId,
                ["media"] = trackId,
                ["option"] = "replace",
            }, cancellationToken);
        }

        return await GetStatusAsync(cancellationToken);
    }

    public async Task<PlaybackStatus> PauseAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync("player_queues/pause", new JsonObject { ["queue_id"] = RequireQueueId() }, cancellationToken);
        return await GetStatusAsync(cancellationToken);
    }

    public async Task<PlaybackStatus> StopAsync(CancellationToken cancellationToken = default)
    {
        await SendAsync("player_queues/stop", new JsonObject { ["queue_id"] = RequireQueueId() }, cancellationToken);
        return await GetStatusAsync(cancellationToken);
    }

    public async Task<PlaybackStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var queue = await SendAsync("player_queues/get", new JsonObject { ["queue_id"] = RequireQueueId() }, cancellationToken);
        var state = queue?["state"]?.GetValue<string>() switch
        {
            "playing" => PlaybackState.Playing,
            "paused" => PlaybackState.Paused,
            _ => PlaybackState.Stopped,
        };

        var track = state == PlaybackState.Stopped ? null : ToTrack(queue?["current_item"]?["media_item"]);
        return new PlaybackStatus(state, track);
    }

    private string RequireQueueId() =>
        string.IsNullOrWhiteSpace(_options.QueueId)
            ? throw new InvalidOperationException("Music:MusicAssistant:QueueId is not configured.")
            : _options.QueueId;

    private async Task<JsonNode?> SendAsync(string command, JsonObject args, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_options.BaseUrl), "api"))
        {
            Content = JsonContent.Create(new JsonObject { ["command"] = command, ["args"] = args }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Music Assistant {Command} failed with {Status}.", command, (int)response.StatusCode);
            throw new HttpRequestException($"Music Assistant {command} failed with status {(int)response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);
    }

    private static Track? ToTrack(JsonNode? item)
    {
        var uri = item?["uri"]?.GetValue<string>();
        var name = item?["name"]?.GetValue<string>();
        if (uri is null || name is null)
        {
            return null;
        }

        var artist = item?["artist_str"]?.GetValue<string>()
            ?? string.Join(", ", item?["artists"]?.AsArray().Select(a => a?["name"]?.GetValue<string>()).OfType<string>() ?? []);
        return new Track(uri, name, artist, item?["album"]?["name"]?.GetValue<string>());
    }
}
