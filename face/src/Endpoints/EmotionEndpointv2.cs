public static class EmotionEndpointv2
{
	public static void MapEmotionV2(this IEndpointRouteBuilder app) =>
		app.MapPut("/v2/state/", (FaceStateRequest request, FaceState face, PoseService poses) =>
		{
			var expression = request.facialExpression!.Trim().ToLowerInvariant();
			var pose = request.pose?.Trim().ToLowerInvariant();

			face.TryApplyExpression(expression);

			return !string.IsNullOrEmpty(pose) && poses.TryApply(pose, out var moves)
				? Results.Ok(new { expression, pose, moves })
				: Results.Ok(new { expression });
		});
}
