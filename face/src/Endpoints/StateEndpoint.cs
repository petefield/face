public static class StateEndpoint
{
	public static void MapState(this IEndpointRouteBuilder app) =>
		app.MapPut("/state", (StateRequest request, PoseService poses) =>
		{
			var pose = request.pose?.Trim().ToLowerInvariant();

			if (poses.TryApply(pose, out var moves))
				return Results.Ok(new { pose, moves });

			return Results.BadRequest(new
			{
				error = string.IsNullOrEmpty(pose) ? "Pose is required." : $"Unknown pose '{pose}'.",
				valid = PoseService.Names
			});
		});
}
