public static class EmotionEndpoint
{
	public static void MapEmotion(this IEndpointRouteBuilder app) =>
		app.MapPost("/emotion/{expression}", (string expression, FaceState face, PoseService poses) =>
		{
			expression = expression.Trim().ToLowerInvariant();

			if (expression.Length == 0)
				return Results.BadRequest(new { error = "Emotion is required." });

			if (!Emotions.All.Contains(expression))
			{
				return Results.BadRequest(new
				{
					error = "Emotion must be one of 'happy', 'angry', 'neutral' or 'sleep'.",
					valid = Emotions.All,
				});
			}

			face.TryApplyExpression(expression);

			return poses.TryApply(expression, out var moves)
				? Results.Ok(new { expression, pose = expression, moves })
				: Results.Ok(new { expression });
		});
}
