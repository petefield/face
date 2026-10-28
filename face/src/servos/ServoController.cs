using System.Device.I2c;
using System.Diagnostics;
using System.Text.Json;
using Iot.Device.Pwm;
using Servos.Core;

public sealed class ServoController : IDisposable
{
    private const int I2cBus = 1;
    private const int Pca9685Address = 0x40;
    private const int PwmFrequencyHz = 50;

    private const double DegreesPerSecond = 45;
    private const double DefaultAngle = 90;
    private const double MinDurationMs = 500;
    private const int StepMs = 20; // one PWM frame at 50 Hz
    private const int IdleReleaseMs = 1000; // stop driving the servo this long after a move ends

    private static readonly string AnglesFile =
        Environment.GetEnvironmentVariable("SERVO_ANGLES_FILE")
        ?? Path.Combine(AppContext.BaseDirectory, "servo-angles.json");

    private readonly object _lock = new();
    private readonly Dictionary<int, double> _currentAngles = [];
    private readonly Dictionary<int, CancellationTokenSource> _moves = [];
    private readonly ILogger<ServoController> _logger;
    private readonly I2cDevice _i2c;
    private readonly Pca9685 _pca;

    public ServoController(ILogger<ServoController> logger)
    {
        _logger = logger;
        _i2c = I2cDevice.Create(new I2cConnectionSettings(I2cBus, Pca9685Address));
        _pca = new Pca9685(_i2c, pwmFrequency: PwmFrequencyHz);

        foreach (var (channel, angle) in LoadAngles())
            _currentAngles[channel] = angle;
    }

    /// <summary>
    /// Starts gliding a servo to the target angle in the background, replacing any move already
    /// running on that channel. Returns the move from the current angle to the target.
    /// </summary>
    public ServoMove Move(int channel, double targetAngle)
    {
        ValidateMove(channel, targetAngle);

        lock (_lock)
        {
            // Resume from wherever an interrupted move got to.
            var from = _currentAngles.GetValueOrDefault(channel, DefaultAngle);

            var cts = ReplaceMove(channel);

            RunInBackground(channel, () => Glide(channel, from, targetAngle, cts.Token));

            return Describe(channel, from, targetAngle);
        }
    }

    /// <summary>
    /// Sets a servo to the target angle immediately, with no easing, replacing any move already
    /// running on that channel. Returns the move from the previous angle to the target.
    /// </summary>
    public ServoMove MoveDirect(int channel, double targetAngle)
    {
        ValidateMove(channel, targetAngle);

        lock (_lock)
        {
            var from = _currentAngles.GetValueOrDefault(channel, DefaultAngle);
            var cts = ReplaceMove(channel);

            SetAngle(channel, targetAngle);
            _currentAngles[channel] = targetAngle;
            SaveAngle(channel, targetAngle);

            RunInBackground(channel, () => ReleaseWhenIdle(channel, cts.Token));

            return Describe(channel, from, targetAngle);
        }
    }

    /// <summary>
    /// Drives a servo with the given pulse width and holds it there. Replaces any move running on
    /// the channel. The equivalent angle is stored for later moves.
    /// </summary>
    public ServoMove SetPulse(int channel, double pulseMs)
    {
        if (!ServoChannels.IsValid(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));

        if (!ServoMath.IsValidPulseMs(pulseMs))
            throw new ArgumentOutOfRangeException(nameof(pulseMs));

        lock (_lock)
        {
            var from = _currentAngles.GetValueOrDefault(channel, DefaultAngle);
            ReplaceMove(channel);
            SetPulseMs(channel, pulseMs);

            var angle = ServoMath.PulseMsToAngle(pulseMs);
            _currentAngles[channel] = angle;
            SaveAngle(channel, angle);

            return Describe(channel, from, angle);
        }
    }

    private ServoMove Describe(int channel, double from, double to)
    {
        var pulseMs = ServoMath.AngleToPulseMs(to);
        return new ServoMove(channel, from, to, pulseMs, ServoMath.PulseMsToDutyCycle(pulseMs, _pca.PwmFrequency));
    }

    private static void ValidateMove(int channel, double angle)
    {
        if (!ServoChannels.IsValid(channel))
            throw new ArgumentOutOfRangeException(nameof(channel));

        if (!ServoMath.IsValidAngle(angle))
            throw new ArgumentOutOfRangeException(nameof(angle));
    }

    // Cancels any move running on the channel and registers a token for the next one. Call with the lock held.
    // The old token source is cancelled but not disposed: the old loop may still be using it.
    private CancellationTokenSource ReplaceMove(int channel)
    {
        if (_moves.Remove(channel, out var previous))
            previous.Cancel();

        var cts = new CancellationTokenSource();
        _moves[channel] = cts;
        return cts;
    }

    private void RunInBackground(int channel, Action work) =>
        _ = Task.Run(work).ContinueWith(
            t => _logger.LogError(t.Exception, "Move on channel {Channel} failed", channel),
            TaskContinuationOptions.OnlyOnFaulted);

    private void Glide(int channel, double from, double to, CancellationToken ct)
    {
        var durationMs = Math.Max(MinDurationMs, Math.Abs(to - from) / DegreesPerSecond * 1000);
        var stopwatch = Stopwatch.StartNew();
        double progress;

        do
        {
            progress = Math.Min(1, stopwatch.Elapsed.TotalMilliseconds / durationMs);
            var angle = from + (to - from) * ServoMath.EaseInOut(progress);

            lock (_lock)
            {
                if (ct.IsCancellationRequested) return;
                SetAngle(channel, angle);
                _currentAngles[channel] = angle;
            }
        }
        while (progress < 1 && !ct.WaitHandle.WaitOne(StepMs));

        if (progress < 1) return; // cancelled by a newer move

        SaveAngle(channel, to);
        ReleaseWhenIdle(channel, ct);
    }

    // Stops the PWM signal so an idle servo doesn't hum. A new move cancels the token and ends the wait early.
    private void ReleaseWhenIdle(int channel, CancellationToken ct)
    {
        if (ct.WaitHandle.WaitOne(IdleReleaseMs)) return;

        lock (_lock)
        {
            if (!ct.IsCancellationRequested)
                _pca.SetDutyCycle(channel, 0);
        }
    }

    private void SetAngle(int channel, double angle) =>
        SetPulseMs(channel, ServoMath.AngleToPulseMs(angle));

    private void SetPulseMs(int channel, double pulseMs) =>
        _pca.SetDutyCycle(channel, ServoMath.PulseMsToDutyCycle(pulseMs, _pca.PwmFrequency));

    private static Dictionary<int, double> LoadAngles()
    {
        try
        {
            return File.Exists(AnglesFile)
                ? JsonSerializer.Deserialize<Dictionary<int, double>>(File.ReadAllText(AnglesFile)) ?? []
                : [];
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return [];
        }
    }

    private void SaveAngle(int channel, double angle)
    {
        lock (_lock)
        {
            var saved = LoadAngles();
            saved[channel] = angle;

            // Write then move, so a crash mid-write can't corrupt the file.
            var temp = AnglesFile + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(saved));
            File.Move(temp, AnglesFile, overwrite: true);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var move in _moves.Values) move.Cancel();
            _pca.Dispose();
        }
        _i2c.Dispose();
    }
}
