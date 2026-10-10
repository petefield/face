
using System.Numerics;

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

app.MapPost("/emotion/{emotionValue}", (string emotionValue, FaceState emotionState) =>
{
	try
	{
		if (string.IsNullOrWhiteSpace(emotionValue))
		{
			return Results.BadRequest(new
			{
				error = "Query parameter 'emotion' is required.",
			});
		}

		emotionValue = emotionValue.ToLowerInvariant();

		if (new string[]{ "happy",  "angry", "neutral","sleep" }.Contains(emotionValue) == false)
		{
			return Results.BadRequest(new
			{
				error = "Query parameter 'emotion' must be one of 'happy', 'angry', 'neutral', or 'sleep'.",
			});
		}

		emotionState.Emotion = emotionValue;

		return Results.Ok(new
		{
			emotion = emotionState.Emotion
		});

	}
	catch (Exception ex)
	{
		return Results.Problem(statusCode: 500, detail: ex.Message);
	}
	
});

app.MapPost("/look/{x:int}/{y:int}", (int x, int y, FaceState emotionState) =>
{
	Vector2 lookDirection = new(x, y);

	emotionState.LookDirection = lookDirection;

	return Results.Ok(new
	{
		lookDirection = emotionState.LookDirection
	});
});



app.Run();