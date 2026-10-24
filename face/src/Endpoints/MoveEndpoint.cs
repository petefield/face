using Servos.Core;

public static class MoveEndpoint
{
	public static void MapMove(this IEndpointRouteBuilder app) =>
		app.MapPost("/move", (MoveRequest request, PoseService poses) =>
		{
			if (request.Pan is null && request.Tilt is null)
				return Results.BadRequest(new { error = "Provide pan and/or tilt." });

			if (!IsValidOrOmitted(request.Pan) || !IsValidOrOmitted(request.Tilt))
			{
				return Results.BadRequest(new
				{
					error = $"Angles must be between 0 and {ServoMath.MaxAngle}.",
					pan = request.Pan,
					tilt = request.Tilt
				});
			}

			return Results.Ok(poses.Apply(new Pose(request.Pan, request.Tilt)));
		});

	private static bool IsValidOrOmitted(double? angle) => angle is null || ServoMath.IsValidAngle(angle.Value);
}
