using Raylib_cs;


public sealed class OwlFace : FaceBase
{    public float LookX { get; set; }
    public float LookY { get; set; }    private float _lookTargetX;
    private float _lookTargetY;
    Dictionary<string, Texture2D> textures;
    private float _blinkTimer;
    private float _nextBlinkIn;
    private bool _blinking;
    private string blinkState;
    private string emotion;

    public OwlFace()
    {
        emotion = "happy";
        blinkState = "open";

        ScheduleNextBlink();
        textures = new Dictionary<string, Texture2D>
        {
            { "neutral-open", Raylib.LoadTexture("./textures/OIP-Neutral.png") },
            { "neutral-closed", Raylib.LoadTexture("./textures/OIP-Netural-Blink.png") },
            { "happy-open", Raylib.LoadTexture("./textures/OIP-Happy.png") },
            { "happy-closed", Raylib.LoadTexture("./textures/OIP-Happy-Blink.png") },
            { "angry-open", Raylib.LoadTexture("./textures/OIP-Angry.png") },
            { "angry-closed", Raylib.LoadTexture("./textures/OIP-Angry-Blink.png") }
        };

    }
    
    public override void Update(float deltaTime, string expression)
    {
        _blinkTimer += deltaTime;
        emotion = expression;
        if (_blinking)
        {
            if (_blinkTimer >= 0.1f)
            {
                _blinking = false;
                blinkState = "open";
                _blinkTimer = 0f;
                ScheduleNextBlink();
            }
        }
        else
        {
            if (_blinkTimer >= _nextBlinkIn)
            {
                _blinking = true;
                blinkState = "closed";
                _blinkTimer = 0f;
            }
        }
    

    
    }

    public override void Draw()
    {
        //Console.WriteLine($"{emotion}-{blinkState}");
        Raylib.DrawTexture(textures[$"{emotion}-{blinkState}"], 0, 0, Color.White);

        if(! _blinking)
        {
              Raylib.DrawEllipse(235, 290, 20, 16, Color.White);
              Raylib.DrawEllipse(410, 290, 20, 16, Color.White);
        }
    }

    private void ScheduleNextBlink()
    {
        _nextBlinkIn = RandomRange(2.5f, 4f);
    }

 
}