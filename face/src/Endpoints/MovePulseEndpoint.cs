using Servos.Core;

public static class MovePulseEndpoint
{
	public static void MapMovePulse(this IEndpointRouteBuilder app) =>
		app.MapPost("/move/pulse/{channel:int}/{pulseMs:double}", (int channel, double pulseMs, ServoController servos) =>
		{
			if (!ServoChannels.IsValid(channel))
				return Results.BadRequest(new { error = $"Channel must be between 0 and {ServoChannels.Max}.", channel });

			if (!ServoMath.IsValidPulseMs(pulseMs))
			{
				return Results.BadRequest(new
				{
					error = $"Pulse width must be between {ServoMath.MinPulseMs} and {ServoMath.MaxPulseMs} ms.",
					pulseMs
				});
			}

			var move = servos.SetPulse(channel, pulseMs);
			return Results.Ok(new { channel, pulseMs, dutyCycle = move.DutyCycle, angle = move.To });
		});
}
