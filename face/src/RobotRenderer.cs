using Raylib_cs;
using System.Numerics;

public class RobotRenderer
    : IDisposable
{
    FaceBase face;

    public RobotRenderer()
    {

        // Raylib.SetConfigFlags(
        //     ConfigFlags.FullscreenMode |
        //     ConfigFlags.VSyncHint |
        //     ConfigFlags.UndecoratedWindow
        // );

        Raylib.InitWindow(640, 480, "Blinky");
        Raylib.HideCursor();
        Raylib.SetTargetFPS(25);
        Raylib.HideCursor();

        face = new OwlFace();
    }

    public void Start(CancellationToken cancellationToken, FaceState faceState)
    {
        while (!cancellationToken.IsCancellationRequested && !Raylib.WindowShouldClose())
        {
            var deltaTime = Raylib.GetFrameTime();
            var expression = faceState.Emotion;

            face.Update(deltaTime, expression, faceState.LookDirection);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            var centre = new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
            Raylib.BeginMode2D(new Camera2D
            {
                Target = centre,
                Offset = centre,
               //Rotation = 180f,
                Zoom = 1f
            });
            face.Draw();
            Raylib.EndMode2D();

            Raylib.EndDrawing();
        }
    }

    public void Dispose()
    {
        Raylib.CloseWindow();
    }

}