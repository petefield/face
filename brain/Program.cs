using System.Text.Json;
using Brain;
using Brain.Skills;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.AddSingleton<OpenAiService>();
builder.Services.Configure<FaceOptions>(builder.Configuration.GetSection(FaceOptions.SectionName));
builder.Services.AddHttpClient<FaceClient>();
builder.Services.AddHttpClient("Speech");
builder.Services.Configure<HomeAssistantOptions>(builder.Configuration.GetSection(HomeAssistantOptions.SectionName));

builder.Services.Configure<GoogleCalendarOptions>(builder.Configuration.GetSection(GoogleCalendarOptions.SectionName));
builder.Services.AddSingleton<GoogleAccessTokenProvider>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSkills();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/prompt", async (PromptRequest request, OpenAiService openAi, FaceClient faceClient, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Prompt))
    {
        return Results.BadRequest("Prompt must not be empty.");
    }

    var response = await openAi.GetResponseAsync(request.Prompt, cancellationToken);
    await faceClient.SetEmotionAsync(response.Emotion, cancellationToken);
    _ = Speak(response.Response, httpClientFactory, app.Lifetime.ApplicationStopping);
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

async Task Speak(string? message, IHttpClientFactory httpClientFactory, CancellationToken cts)
{
    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    if(string.IsNullOrWhiteSpace(message))
        return;    

    try
    {
        using var http = httpClientFactory.CreateClient("Speech");
        using var response = await http.PostAsJsonAsync(
            "http://localhost:5060/speak",
            new { text = message, speed = 180 },
            jsonOptions,
            cts);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cts);
            Console.WriteLine($"error> speak failed: {(int)response.StatusCode} {response.ReasonPhrase}: {body}", ConsoleColor.Red);
        }
    }
    catch (OperationCanceledException) when (cts.IsCancellationRequested)
    {
    }
    catch (Exception ex)
    {
        Console.WriteLine($"error> {ex.Message}", ConsoleColor.Red);
    }
}