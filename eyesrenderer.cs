using Raylib_cs;
using System.Numerics;

public sealed class EyesRenderer
{
    public float LookX { get; set; }
    public float LookY { get; set; }

    private const float PupilRadius = 25;
    private const float EyeWidth = 180;
    private const float EyeHeight = 160;
    private static readonly Color IrisColor = new(80, 180, 255, 255);

    private const float MaxPupilTravelX = 35;
    private const float MaxPupilTravelY = 20;
    private readonly Random _random = new();

    private float _lookTargetX;
    private float _lookTargetY;
    private float _saccadeTimer;
    private float _nextSaccadeIn;

    private float _time;
private const float BreathingSpeed = 0.8f;
private const float BreathingAmount = 3f;

    private const float SaccadeSpeed = 12f;

        private const float MouthWidth = 90;
        private const float MouthHeight = 35;
        private const float MouthThickness = 6;

    private float _blinkTimer;
    private float _nextBlinkIn;

    private bool _blinking;
    private float _blinkAmount;

    public EyesRenderer()
    {
        ScheduleNextBlink();
        ScheduleNextSaccade();
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
        _saccadeTimer += deltaTime;

      

        if (_saccadeTimer >= _nextSaccadeIn)
        {
            _lookTargetX = RandomRange(-0.25f, 0.25f);
            _lookTargetY = RandomRange(-0.15f, 0.15f);

            _saccadeTimer = 0f;
            ScheduleNextSaccade();
        }

        LookX = SmoothTowards(LookX, _lookTargetX, SaccadeSpeed * deltaTime);
        LookY = SmoothTowards(LookY, _lookTargetY, SaccadeSpeed * deltaTime);


        if (!_blinking)
        {
            _blinkTimer += deltaTime;

            if (_blinkTimer >= _nextBlinkIn)
            {
                _blinking = true;
                _blinkTimer = 0;
            }
        }
        else
        {
            _blinkTimer += deltaTime;

            const float blinkDuration = 0.15f;

            var progress = _blinkTimer / blinkDuration;

            if (progress < 0.5f)
            {
                _blinkAmount = progress * 2f;
            }
            else
            {
                _blinkAmount = 2f - (progress * 2f);
            }

            if (progress >= 1f)
            {
                _blinking = false;
                _blinkAmount = 0f;
                _blinkTimer = 0f;

                ScheduleNextBlink();
            }
        }
    }

    public void Draw()
    {

        var breathY = MathF.Sin(_time * BreathingSpeed) * BreathingAmount;

        var offset = new Vector2(0, breathY);

        var leftEye = new Vector2(250, 220) + offset;
        var rightEye = new Vector2(550, 220) + offset;
        var mouth = new Vector2(400, 350) + offset;

        DrawEye(leftEye);
        DrawEye(rightEye);

        DrawEyebrow(leftEye);
        DrawEyebrow(rightEye);

        DrawMouth(mouth);
    }

    private void DrawEye(Vector2 centre)
    {
        var currentEyeHeight = EyeHeight * (1f - _blinkAmount);
        currentEyeHeight = MathF.Max(currentEyeHeight, 8f);

        // Eye white squashes during blink
        Raylib.DrawEllipse(
            (int)centre.X,
            (int)centre.Y,
            EyeWidth / 2,
            currentEyeHeight / 2,
            Color.White);

        var pupilOffset = new Vector2(
            LookX * MaxPupilTravelX,
            LookY * MaxPupilTravelY);

        var pupilCentre = centre + pupilOffset;

        // Keep pupil round
        if (_blinkAmount < 0.65f)
        {
            DrawPupil(pupilCentre);
        }

        // Closed-eye line
        if (_blinkAmount > 0.8f)
        {
            Raylib.DrawLineEx(
                new Vector2(centre.X - EyeWidth / 2, centre.Y),
                new Vector2(centre.X + EyeWidth / 2, centre.Y),
                6,
                Color.White);
        }
    }

    private void DrawEyebrow(Vector2 eyeCentre)
    {
        var eyebrowY = eyeCentre.Y - EyeHeight * 0.65f;

        var left = new Vector2(
            eyeCentre.X - EyeWidth * 0.25f,
            eyebrowY);

        var right = new Vector2(
            eyeCentre.X + EyeWidth * 0.25f,
            eyebrowY);

        Raylib.DrawLineEx(
            left,
            right,
            8,
            Color.White);
    }

    private void DrawMouth(Vector2 centre)
    {
        const int segments = 24;

        Vector2? previous = null;

        for (var i = 0; i <= segments; i++)
        {
            var t = i / (float)segments;

            // t: 0 -> 1 across the mouth
            var x = (t - 0.5f) * MouthWidth;

            // Smile curve with a sine wave, so it looks more natural when animating
            var y = (MathF.Sin(t * MathF.PI) * MouthHeight) * (0.5f);

            var point = new Vector2(
                centre.X + x,
                centre.Y + y);

            if (previous is not null)
            {
                Raylib.DrawLineEx(
                    previous.Value,
                    point,
                    MouthThickness,
                    Color.White);
            }

            previous = point;
        }
    }

private static readonly Color IrisColour =
    new(80, 220, 255, 255);

private void DrawPupil(Vector2 centre)
{
    // Outer glow
    Raylib.DrawCircleV(
        centre,
        40,
        new Color(
            (byte)IrisColour.R,
            (byte)IrisColour.G,
            (byte)IrisColour.B,
            (byte)25));

    // Mid glow
    Raylib.DrawCircleV(
        centre,
        30,
        new Color(
            (byte)IrisColour.R,
            (byte)IrisColour.G,
            (byte)IrisColour.B,
            (byte)60));

    // Iris
    Raylib.DrawCircleV(
        centre,
        22,
        IrisColour);

    // Pupil
    Raylib.DrawCircleV(
        centre,
        10,
        Color.Black);

    // Highlight
    Raylib.DrawCircleV(
        centre + new Vector2(-4, -4),
        2,
        Color.White);
}


    private void ScheduleNextBlink()
    {
        _nextBlinkIn = RandomRange(2.5f, 4f);
    }

    private void ScheduleNextSaccade()
    {
        _nextSaccadeIn = RandomRange(0.8f, 2.5f);
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }

    private static float SmoothTowards(float current, float target, float amount)
    {
        return current + (target - current) * MathF.Min(amount, 1f);
    }

}