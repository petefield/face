using Blinky.McpServer;
using Blinky.McpServer.Music;
using Brain.Skills;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<TimersOptions>(builder.Configuration.GetSection(TimersOptions.SectionName));
builder.Services.AddSingleton<TimersFile>();
builder.Services.Configure<GoogleCalendarOptions>(builder.Configuration.GetSection(GoogleCalendarOptions.SectionName));
builder.Services.AddSingleton<GoogleAccessTokenProvider>();
builder.Services.Configure<XweatherOptions>(builder.Configuration.GetSection(XweatherOptions.SectionName));
builder.Services.AddHttpClient(XweatherOptions.HttpClientName, client =>
{
    client.BaseAddress = new Uri("https://data.api.xweather.com/");
    client.Timeout = TimeSpan.FromSeconds(15);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
.RemoveAllLoggers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSkills();
builder.Services.Configure<HomeAssistantOptions>(builder.Configuration.GetSection(HomeAssistantOptions.SectionName));

builder.Services.Configure<MusicOptions>(builder.Configuration.GetSection(MusicOptions.SectionName));
builder.Services.Configure<MusicAssistantOptions>(builder.Configuration.GetSection(MusicAssistantOptions.SectionName));
builder.Services.AddHttpClient(MusicAssistantOptions.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<IMusicService, MusicAssistantMusicService>();

builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<MusicTools>()
    .WithSkillTools();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapMcp("/mcp");

await app.Services.GetRequiredService<IMusicService>().StopAsync();

var music = app.MapGroup("/music").WithTags("Music");
music.MapGet("/search", (string q, IMusicService svc, CancellationToken ct) => svc.SearchAsync(q, cancellationToken: ct)).WithName("SearchMusic");
music.MapGet("/status", (IMusicService svc, CancellationToken ct) => svc.GetStatusAsync(ct)).WithName("MusicStatus");
music.MapPost("/play", async (string? trackId, IMusicService svc, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await svc.PlayAsync(trackId, ct));
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(ex.Message);
    }
}).WithName("PlayMusic");
music.MapPost("/pause", (IMusicService svc, CancellationToken ct) => svc.PauseAsync(ct)).WithName("PauseMusic");
music.MapPost("/stop", (IMusicService svc, CancellationToken ct) => svc.StopAsync(ct)).WithName("StopMusic");

app.Run();
