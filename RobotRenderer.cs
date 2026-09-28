using Raylib_cs;

public class RobotRenderer
    : IDisposable
{
    Face face;

    public RobotRenderer()
    {
        Raylib.InitWindow(800, 480, "Henry 1");
        Raylib.ToggleBorderlessWindowed();

        Raylib.ToggleFullscreen();

        Raylib.SetTargetFPS(60);
        Raylib.HideCursor();

        face = new Face();
    }


public static Dictionary<string, FacialExpression> Emotions { get; } = new Dictionary<string, FacialExpression>(StringComparer.OrdinalIgnoreCase)
    {
        { "Happy", new FacialExpression() { EyebrowHeight = 25, EyeHeight = 190} },
        { "Sad", new FacialExpression() { EyebrowHeight = 40, EyeHeight = 150} },
        { "Angry", new FacialExpression() { EyebrowHeight = 20, EyeHeight = 100} },
        { "Surprised", new FacialExpression() { EyebrowHeight = 30, EyeHeight = 200} },
        { "Neutral", new FacialExpression() { EyebrowHeight = 35, EyeHeight = 170} }
    };
    
    public void Start(CancellationToken cancellationToken, EmotionState emotionState)
    {
        while (!cancellationToken.IsCancellationRequested && !Raylib.WindowShouldClose())
        {
            var deltaTime = Raylib.GetFrameTime();
            var emotion = emotionState.Emotion;

            if (!Emotions.TryGetValue(emotion, out var expression))
            {
                expression = Emotions["Neutral"];
            }

            face.Update(deltaTime, expression);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            face.Draw();

            Raylib.EndDrawing();
        }
    }

    public void Dispose()
    {
        Raylib.CloseWindow();
    }

}