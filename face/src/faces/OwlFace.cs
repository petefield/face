using System.Numerics;
using Raylib_cs;

public sealed class OwlFace : FaceBase
{
    private static readonly Vector2 LeftEye = new(235, 290);
    private static readonly Vector2 RightEye = new(410, 290);

    // How far (in pixels) the pupils travel at full deflection.
    private const float LookRangeX = 14f;
    private const float LookRangeY = 9f;
    private const float LookEasing = 8f;

    private const float BlinkDuration = 0.1f;
    private const float MinBlinkInterval = 2.5f;
    private const float MaxBlinkInterval = 4f;

    private readonly Dictionary<string, Texture2D> _textures = new()
    {
        ["neutral-open"] = Raylib.LoadTexture("./textures/OIP-Neutral.png"),
        ["neutral-closed"] = Raylib.LoadTexture("./textures/OIP-Netural-Blink.png"),
        ["happy-open"] = Raylib.LoadTexture("./textures/OIP-Happy.png"),
        ["happy-closed"] = Raylib.LoadTexture("./textures/OIP-Happy-Blink.png"),
        ["angry-open"] = Raylib.LoadTexture("./textures/OIP-Angry.png"),
        ["angry-closed"] = Raylib.LoadTexture("./textures/OIP-Angry-Blink.png"),
        ["sleep-open"] = Raylib.LoadTexture("./textures/OIP-Sleep.png"),
        ["sleep-closed"] = Raylib.LoadTexture("./textures/OIP-Sleep.png"),
    };

    private Vector2 _currentLook;
    private float _blinkTimer;
    private float _nextBlinkIn;
    private bool _blinking;
    private string _emotion = "neutral";

    public OwlFace()
    {
        _nextBlinkIn = RandomRange(MinBlinkInterval, MaxBlinkInterval);
    }

    public override void Update(float deltaTime, string emotion, Vector2 lookDirection)
    {
        _blinkTimer += deltaTime;
        _emotion = emotion;

        // Framerate-independent exponential ease towards the requested direction.
        float t = 1f - MathF.Exp(-LookEasing * deltaTime);
        _currentLook = new Vector2(
            SmoothTowards(_currentLook.X, lookDirection.X, t),
            SmoothTowards(_currentLook.Y, lookDirection.Y, t));

        if (emotion == "sleep")
            return;

        if (_blinking)
        {
            if (_blinkTimer >= BlinkDuration)
            {
                _blinking = false;
                _blinkTimer = 0f;
                _nextBlinkIn = RandomRange(MinBlinkInterval, MaxBlinkInterval);
            }
        }
        else
        {
            if (_blinkTimer >= _nextBlinkIn)
            {
                _blinking = true;
                _blinkTimer = 0f;
            }
        }
    }

    public override void Draw()
    {
        var textureKey = $"{_emotion}-{(_blinking ? "closed" : "open")}";

        Raylib.DrawTexture(_textures[textureKey], 0, 0, Color.White);

        Vector2 screenCentre = new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
        Vector2 easedTarget = screenCentre + _currentLook;

        if (!_blinking)
        {
            Vector2 left = LeftEye + PupilOffset(LeftEye, easedTarget);
            Vector2 right = RightEye + PupilOffset(RightEye, easedTarget);

            Raylib.DrawEllipse((int)left.X, (int)left.Y, 20, 16, Color.White);
            Raylib.DrawEllipse((int)right.X, (int)right.Y, 20, 16, Color.White);
        }
    }

    // Aims a pupil at the target, limited to the elliptical travel range of its socket.
    private static Vector2 PupilOffset(Vector2 eyeCentre, Vector2 target)
    {
        Vector2 toTarget = target - eyeCentre;
        if (toTarget.LengthSquared() < 0.0001f)
        {
            return Vector2.Zero;
        }

        Vector2 direction = Vector2.Normalize(toTarget);
        Vector2 maxOffset = new Vector2(direction.X * LookRangeX, direction.Y * LookRangeY);
        return toTarget.Length() < maxOffset.Length() ? toTarget : maxOffset;
    }
}