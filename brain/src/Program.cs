using System.Text.Json;
using Brain;
using Brain.Skills;
using Brain.Skills.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddHttpClient(SpeechService.HttpClientName);
builder.Services.AddSingleton<SpeechService>();
builder.Services.Configure<TimersOptions>(builder.Configuration.GetSection(TimersOptions.SectionName));
builder.Services.AddSingleton<TimersFile>();
builder.Services.AddSingleton<TimerEvents>();
builder.Services.AddHostedService<TimerWatcherService>();
builder.Services.AddHostedService<TimerAlertService>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSkills();
builder.Services.Configure<McpOptions>(builder.Configuration.GetSection(McpOptions.SectionName));
builder.Services.AddSingleton<McpSkillProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/prompt", async (PromptRequest request, OpenAiService openAi, SpeechService speech, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Prompt))
    {
        return Results.BadRequest("Prompt must not be empty.");
    }

    var response = await openAi.GetResponseAsync(request.Prompt, cancellationToken);
    _ = speech.SpeakAsync(response.Response, app.Lifetime.ApplicationStopping);
    return Results.Ok(response);
})
.WithName("SendPrompt");

app.MapPost("/prompt/reset", async (OpenAiService openAi, CancellationToken cancellationToken) =>
{
    await openAi.ResetHistoryAsync(cancellationToken);
    return Results.NoContent();
})
.WithName("ResetPromptHistory");

Console.WriteLine("All Good. Brain Running!");

app.Run();
