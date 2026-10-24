using System.ComponentModel.DataAnnotations;

/// <summary>Validates that an optional pose name exists in <see cref="PoseService"/>.</summary>
public sealed class KnownPoseAttribute : ValidationAttribute
{
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (value is not string name || string.IsNullOrWhiteSpace(name) || PoseService.TryGetPose(name, out _))
			return ValidationResult.Success;

		return new ValidationResult(
			$"Unknown pose '{name}'. Valid poses: {string.Join(", ", PoseService.Names)}.",
			validationContext.MemberName is { } member ? [member] : null);
	}
}
