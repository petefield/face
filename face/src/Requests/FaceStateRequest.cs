using System.ComponentModel.DataAnnotations;

public record FaceStateRequest(
	[Required, AllowedValues("happy", "angry", "neutral", "sleep")] string? facialExpression,
	[KnownPose] string? pose);
