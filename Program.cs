using Raylib_cs;


Raylib.InitWindow(800, 480, "Robot Face");
Raylib.SetTargetFPS(60);

var eyes = new EyesRenderer();

while (!Raylib.WindowShouldClose())
{
    var deltaTime = Raylib.GetFrameTime();

    eyes.Update(deltaTime);

    Raylib.BeginDrawing();
    Raylib.ClearBackground(Color.Black);

    eyes.Draw();

    Raylib.EndDrawing();
}


Raylib.CloseWindow();