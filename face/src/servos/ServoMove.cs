/// <summary>Result of a servo move: the angles moved between and the PWM signal for the target.</summary>
public sealed record ServoMove(int Channel, double From, double To, double PulseMs, double DutyCycle);
