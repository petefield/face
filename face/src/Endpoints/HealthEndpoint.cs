public static class HealthEndpoint
{
	public static void MapHealth(this IEndpointRouteBuilder app) =>
		app.MapGet("/health", () => Results.Ok("ok"));
}
