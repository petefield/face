
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EmotionState>();
builder.Services.AddHostedService<RobotRendererService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
	app = "face",
	status = "running"
}));

app.MapGet("/health", () => Results.Ok("ok"));

app.MapPost("/emotion", (string emotion, EmotionState emotionState) =>
{
	if (string.IsNullOrWhiteSpace(emotion))
	{
		return Results.BadRequest(new
		{
			error = "Query parameter 'emotion' is required.",
			allowed = RobotRenderer.Emotions.Keys
		});
	}

	if (!RobotRenderer.Emotions.ContainsKey(emotion))
	{
		return Results.BadRequest(new
		{
			error = $"Unknown emotion '{emotion}'.",
			allowed = RobotRenderer.Emotions.Keys
		});
	}

	emotionState.Emotion = emotion;

	return Results.Ok(new
	{
		emotion = emotionState.Emotion
	});
});

app.Run();