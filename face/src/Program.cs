
using System.Drawing.Text;
using System.Linq.Expressions;
using System.Numerics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FaceState>();
builder.Services.AddSingleton<ServoController>();
builder.Services.AddSingleton<PoseService>();
builder.Services.AddHostedService<RobotRendererService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
	app = "face",
	status = "running"
}));

app.MapGet("/health", () => Results.Ok("ok"));

app.MapPost("/emotion/{emotionValue}", (string emotionValue, FaceState emotionState, PoseService poses) =>
{
	try
	{
		if (string.IsNullOrWhiteSpace(emotionValue))
		{
			return Results.BadRequest(new
			{
				error = "Emotion is required.",
			});
		}

		emotionValue = emotionValue.ToLowerInvariant();

		if (new string[]{ "happy",  "angry", "neutral","sleep" }.Contains(emotionValue) == false)
		{
			return Results.BadRequest(new
			{
				error = "Emotion must be one of 'happy', 'angry', 'neutral' or 'sleep'.",
				valid = new[] { "happy", "angry", "neutral", "sleep" },
			});
		}

		emotionState.Emotion = emotionValue;

		if (poses.TryApply(emotionValue, out var moves))
		{
			return Results.Ok(new
			{
				emotion = emotionState.Emotion,
				pose = emotionValue,
				moves
			});
		}

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

app.MapPost("/move/{channel:int}/{angle:int}", (int channel, int angle, ServoController servoController) =>
{
	try
	{
		if (channel is < 0 or > 15)
			return Results.BadRequest(new { error = "Channel must be between 0 and 15.", channel });

		if (angle is < 0 or > 180)
			return Results.BadRequest(new { error = "Angle must be between 0 and 180.", angle });

		var (from, to) = servoController.Move(channel, angle);
		return Results.Ok(new { channel, from, to });
	}
	catch (Exception ex)
	{
		return Results.Problem(statusCode: 500, detail: ex.Message);
	}
});

app.MapPost("/move", (MoveRequest request, PoseService poses) =>
{
	try
	{
		if (request.Pan is null && request.Tilt is null)
			return Results.BadRequest(new { error = "Provide pan and/or tilt." });

		if (request.Pan is < 0 or > 180 || request.Tilt is < 0 or > 180)
			return Results.BadRequest(new { error = "Angles must be between 0 and 180.", pan = request.Pan, tilt = request.Tilt });

		return Results.Ok(poses.Apply(new Pose(request.Pan, request.Tilt)));
	}
	catch (Exception ex)
	{
		return Results.Problem(statusCode: 500, detail: ex.Message);
	}
});

app.MapPut("/state", (StateRequest request, PoseService poses) =>
{
	try
	{
		var pose = request.pose?.Trim().ToLowerInvariant();

		if (!poses.TryApply(pose, out var moves))
		{
			return Results.BadRequest(new
			{
				error = string.IsNullOrEmpty(pose) ? "Pose is required." : $"Unknown pose '{pose}'.",
				valid = PoseService.Names
			});
		}

		return Results.Ok(new { pose, moves });
	}
	catch (Exception ex)
	{
		return Results.Problem(statusCode: 500, detail: ex.Message);
	}
});

app.Run();

public record MoveRequest(double? Pan, double? Tilt);

public record StateRequest(string? pose);
