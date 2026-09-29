
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FaceState>();
builder.Services.AddHostedService<RobotRendererService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
	app = "face",
	status = "running"
}));

app.MapGet("/health", () => Results.Ok("ok"));

app.MapPost("/emotion", (string emotion, FaceState emotionState) =>
{
	if (string.IsNullOrWhiteSpace(emotion))
	{
		return Results.BadRequest(new
		{
			error = "Query parameter 'emotion' is required.",
		});
	}

	emotionState.Emotion = emotion;

	return Results.Ok(new
	{
		emotion = emotionState.Emotion
	});
});

app.Run();