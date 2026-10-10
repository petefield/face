using System.Numerics;
using Raylib_cs;

public sealed class OwlFace : FaceBase
{   
    Dictionary<string, Texture2D> textures= new Dictionary<string, Texture2D>
    {
        { "neutral-open", Raylib.LoadTexture("./textures/OIP-Neutral.png") },
        { "neutral-closed", Raylib.LoadTexture("./textures/OIP-Netural-Blink.png") },
        { "happy-open", Raylib.LoadTexture("./textures/OIP-Happy.png") },
        { "happy-closed", Raylib.LoadTexture("./textures/OIP-Happy-Blink.png") },
        { "angry-open", Raylib.LoadTexture("./textures/OIP-Angry.png") },
        { "angry-closed", Raylib.LoadTexture("./textures/OIP-Angry-Blink.png") },
        { "sleep-open", Raylib.LoadTexture("./textures/OIP-Sleep.png") },
        { "sleep-closed", Raylib.LoadTexture("./textures/OIP-Sleep.png") }

    };

    private Vector2 _lookDirection;
    private Vector2 _currentLook;
    private Vector2 leftEye = new Vector2(235, 290);
    private Vector2 rightEye =  new Vector2(410, 290);

    // How far (in pixels) the pupils travel at full deflection.
    private const float LookRangeX = 14f;
    private const float LookRangeY = 9f;
    private const float LookEasing = 8f;
    
    private float _blinkTimer;
    private float _nextBlinkIn;
    private bool _blinking;
    private string _emotion;

    public OwlFace()
    {
        _emotion = "neutral";
        _nextBlinkIn = RandomRange(2.5f, 4f);
    }
    
    public override void Update(float deltaTime, string emotion, Vector2 lookDirection)
    {
        _blinkTimer += deltaTime;
        _emotion =emotion;
        _lookDirection = lookDirection;

        // Framerate-independent exponential ease towards the requested direction.
        float t = 1f - MathF.Exp(-LookEasing * deltaTime);
        _currentLook = new Vector2(
            SmoothTowards(_currentLook.X, _lookDirection.X, t),
            SmoothTowards(_currentLook.Y, _lookDirection.Y, t));

        if(emotion == "sleep")
        {
            return;
        }

        if (_blinking)
        {
            if (_blinkTimer >= 0.1f)
            {
                _blinking = false;
                _blinkTimer = 0f;
                _nextBlinkIn = RandomRange(2.5f, 4f);
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

        var textureKey = $"{(_emotion)}-{(_blinking ? "closed" : "open")}";

        Raylib.DrawTexture(textures[textureKey], 0, 0, Color.White);

        Vector2 screenCentre = new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
        Vector2 easedTarget = screenCentre + _currentLook;

        if(! _blinking)
        {
            Vector2 left = leftEye + PupilOffset(leftEye, easedTarget);
            Vector2 right = rightEye + PupilOffset(rightEye, easedTarget);

            Raylib.DrawEllipse((int)left.X, (int)left.Y, 20, 16, Color.White);
            Raylib.DrawEllipse((int)right.X, (int)right.Y, 20, 16, Color.White);
        }

        // Debug marker showing where the face is being told to look.
     //   Raylib.DrawCircle((int)easedTarget.X, (int)easedTarget.Y, 20f, Color.Green);
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