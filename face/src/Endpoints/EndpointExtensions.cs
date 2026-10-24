public static class EndpointExtensions
{
	public static void MapEndpoints(this IEndpointRouteBuilder app)
	{
		var api = app.MapGroup("").AddEndpointFilter(ProblemOnException);

		api.MapRoot();
		api.MapHealth();
		api.MapEmotion();
		api.MapEmotionV2();
		api.MapMoveChannel();
		api.MapMoveDirect();
		api.MapMovePulse();
		api.MapMove();
		api.MapState();
	}

	// Turns any unhandled exception into a 500 problem response.
	private static async ValueTask<object?> ProblemOnException(
		EndpointFilterInvocationContext context,
		EndpointFilterDelegate next)
	{
		try
		{
			return await next(context);
		}
		catch (Exception ex)
		{
			return Results.Problem(statusCode: 500, detail: ex.Message);
		}
	}
}
