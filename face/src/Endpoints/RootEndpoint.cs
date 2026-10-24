public static class RootEndpoint
{
	public static void MapRoot(this IEndpointRouteBuilder app) =>
		app.MapGet("/", () => Results.Ok(new
		{
			app = "face",
			status = "running"
		}));
}
