using System.Diagnostics.CodeAnalysis;

public static class ServoChannels
{
    public const int Pan = 15;
    public const int Tilt = 14;
    public const int Max = 15;

    public static bool IsValid(int channel) => channel is >= 0 and <= Max;
}

/// <summary>Target angles for the pan and tilt servos. A null angle leaves that servo where it is.</summary>
public sealed record Pose(double? Pan, double? Tilt);

public sealed class PoseService(ServoController servos)
{
    private static readonly Dictionary<string, Pose> Poses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sleep"] = new Pose(Pan: 90, Tilt: 120),
        ["happy"] = new Pose(Pan: 90, Tilt: 90)
    };

    public static IReadOnlyCollection<string> Names => Poses.Keys;

    public static bool TryGetPose(string? name, [NotNullWhen(true)] out Pose? pose)
    {
        pose = null;
        return name is not null && Poses.TryGetValue(name.Trim(), out pose);
    }

    /// <summary>Starts the named pose and returns the move for each servo, or false if the pose is unknown.</summary>
    public bool TryApply(string? name, [NotNullWhen(true)] out object? result)
    {
        result = null;
        if (!TryGetPose(name, out var pose)) return false;

        result = Apply(pose);
        return true;
    }

    /// <summary>Starts moving the servos to the pose; moves run in the background.</summary>
    public object Apply(Pose pose)
    {
        ServoMove? Run(int channel, double? angle) =>
            angle is { } a ? servos.Move(channel, a) : null;

        return new
        {
            pan = Run(ServoChannels.Pan, pose.Pan),
            tilt = Run(ServoChannels.Tilt, pose.Tilt),
        };
    }
}
