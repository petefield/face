namespace Servos.Core;

public static class ServoMath
{
    public const int MaxAngle = 180;

    // Pulse widths this servo needs for 0°, the centre and MaxAngle°. The centre is calibrated
    // separately because it isn't exactly halfway between the ends.
    public const double MinPulseMs = 0.5;
    public const double CenterPulseMs = 1.54;
    public const double MaxPulseMs = 2.7;

    private const double CenterAngle = MaxAngle / 2.0;

    /// <summary>True for a real number in [0, MaxAngle]; NaN is rejected.</summary>
    public static bool IsValidAngle(double angle) => angle is >= 0 and <= MaxAngle;

    /// <summary>True for a real number in [MinPulseMs, MaxPulseMs]; NaN is rejected.</summary>
    public static bool IsValidPulseMs(double pulseMs) => pulseMs is >= MinPulseMs and <= MaxPulseMs;

    /// <summary>Maps an angle to a pulse width in milliseconds, piecewise linear through the calibrated centre.</summary>
    public static double AngleToPulseMs(double angle)
    {
        if (!IsValidAngle(angle)) throw new ArgumentOutOfRangeException(nameof(angle));

        return angle <= CenterAngle
            ? MinPulseMs + (CenterPulseMs - MinPulseMs) * angle / CenterAngle
            : CenterPulseMs + (MaxPulseMs - CenterPulseMs) * (angle - CenterAngle) / (MaxAngle - CenterAngle);
    }

    /// <summary>Inverse of <see cref="AngleToPulseMs"/>, clamped to the valid angle range.</summary>
    public static double PulseMsToAngle(double pulseMs)
    {
        var angle = pulseMs <= CenterPulseMs
            ? (pulseMs - MinPulseMs) / (CenterPulseMs - MinPulseMs) * CenterAngle
            : CenterAngle + (pulseMs - CenterPulseMs) / (MaxPulseMs - CenterPulseMs) * (MaxAngle - CenterAngle);

        return Math.Clamp(angle, 0, MaxAngle);
    }

    /// <summary>Fraction of each PWM period the signal is high for the given pulse width.</summary>
    public static double PulseMsToDutyCycle(double pulseMs, double frequencyHz) => pulseMs / 1000.0 * frequencyHz;

    /// <summary>Ease-in-out (smoothstep) for t in [0,1].</summary>
    public static double EaseInOut(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }
}
