using Servos.Core;

public static class MoveDirectEndpoint
{
	public static void MapMoveDirect(this IEndpointRouteBuilder app) =>
		app.MapPost("/move/direct/{channel:int}/{angle:int}", (int channel, int angle, ServoController servos) =>
		{
			if (!ServoChannels.IsValid(channel))
				return Results.BadRequest(new { error = $"Channel must be between 0 and {ServoChannels.Max}.", channel });

			if (!ServoMath.IsValidAngle(angle))
				return Results.BadRequest(new { error = $"Angle must be between 0 and {ServoMath.MaxAngle}.", angle });

			return Results.Ok(servos.MoveDirect(channel, angle));
		});
}
