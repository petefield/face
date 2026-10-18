namespace Servos.Core;

public static class ServoMath
{
    public const int MaxAngle = 180;

    public static bool TryParseAngle(string? text, out int angle) =>
        int.TryParse(text, out angle) && angle is >= 0 and <= MaxAngle;

    /// <summary>Maps an angle to a pulse width in microseconds.</summary>
    public static double AngleToPulseMicroseconds(double angle, double minPulse = 750, double maxPulse = 2250)
    {
        if (angle is < 0 or > MaxAngle) throw new ArgumentOutOfRangeException(nameof(angle));
        return minPulse + (maxPulse - minPulse) * angle / MaxAngle;
    }

    /// <summary>Ease-in-out (smoothstep) for t in [0,1].</summary>
    public static double EaseInOut(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - 2 * t);
    }
}
