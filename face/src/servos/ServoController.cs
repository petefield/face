
using System.Device.I2c;
using System.Text.Json;
using Iot.Device.Pwm;
using Servos.Core;

public sealed class ServoController : IDisposable
{
    private readonly object _lock = new();
    private readonly Dictionary<int, double> currentAngles = [];
    private readonly Dictionary<int, CancellationTokenSource> _cts = [];

    private const double DegreesPerSecond = 45;
    private const double DefaultAngle = 90;
    private const double MinDurationMs = 500;
    private const int IdleReleaseMs = 1000; // stop driving the servo this long after a move ends
    private const int StepMs = 20; // one PWM frame at 50 Hz



    private static readonly string AnglesFile =
        Path.Combine(AppContext.BaseDirectory, "servo-angles.json");

    private readonly ILogger<ServoController> _logger;
    private readonly I2cDevice _i2c;
    private readonly Pca9685 _pca;

    public ServoController(ILogger<ServoController> logger)
    {
        _logger = logger;
        _i2c = I2cDevice.Create(
            new I2cConnectionSettings(1, 0x40));

        _pca = new Pca9685(_i2c, pwmFrequency: 50);

        foreach (var (channel, angle) in LoadAngles())
            currentAngles[channel] = angle;
    }

    public (double, double) Move(int channel, double targetAngle)
    {
        if (channel < 0 || channel > 15)
            throw new ArgumentOutOfRangeException(nameof(channel));

        if (double.IsNaN(targetAngle) || targetAngle < 0 || targetAngle > 180)
            throw new ArgumentOutOfRangeException(nameof(targetAngle));

        targetAngle = Math.Clamp(targetAngle, 0, ServoMath.MaxAngle);

        lock (_lock)
        {
            // Resume from wherever an interrupted move got to.
            if (!currentAngles.TryGetValue(channel, out var currentAngle))
            {
                currentAngle = DefaultAngle;
                currentAngles[channel] = currentAngle;
            }

            if (_cts.Remove(channel, out var previous))
            {
                // Not disposed: the old loop may still be using its token.
                previous.Cancel();
            }

            var cts = new CancellationTokenSource();
            _cts[channel] = cts;
            _ = Task.Run(() => Glide(channel, currentAngle, targetAngle, cts.Token))
                .ContinueWith(
                    t => _logger.LogError(t.Exception, "Move on channel {Channel} failed", channel),
                    TaskContinuationOptions.OnlyOnFaulted);
            return (currentAngle, targetAngle);
        }
    }

    private void Glide(int channel, double from, double to, CancellationToken ct)
    {
        double distance = Math.Abs(to - from);
        double durationMs = Math.Max(MinDurationMs, distance / DegreesPerSecond * 1000);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (!ct.IsCancellationRequested)
        {
            double t = Math.Min(1, sw.Elapsed.TotalMilliseconds / durationMs);
            double angle = from + (to - from) * ServoMath.EaseInOut(t);

            lock (_lock)
            {
                if (ct.IsCancellationRequested) return;
                SetAngle(channel, angle);
                currentAngles[channel] = angle;
            }

            if (t >= 1)
            {
                SaveAngle(channel, to);

                // A new move cancels the token, which ends the wait early.
                if (!ct.WaitHandle.WaitOne(IdleReleaseMs))
                {
                    lock (_lock)
                    {
                        if (!ct.IsCancellationRequested)
                            _pca.SetDutyCycle(channel, 0);
                    }
                }
                return;
            }
            ct.WaitHandle.WaitOne(StepMs);
        }
    }

    private Dictionary<int, double> LoadAngles()
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

    private void SetAngle(int channel, double angle)
    {
        double pulseUs = ServoMath.AngleToPulseMicroseconds(angle);
        _pca.SetDutyCycle(channel, pulseUs * _pca.PwmFrequency / 1_000_000.0);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var c in _cts.Values) c.Cancel();
            _pca.Dispose();
        }
        _i2c.Dispose();
    }
}
