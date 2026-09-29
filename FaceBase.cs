using Raylib_cs;
using System.Linq.Expressions;
using System.Numerics;

public abstract class FaceBase
{
    protected readonly Random _random = new();

    public abstract void Update(float deltaTime, string expression, Vector2 lookDirection);

    public abstract void Draw();  
     
   
    protected float RandomRange(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }

    protected static float SmoothTowards(float current, float target, float amount)
    {
        return current + (target - current) * MathF.Min(amount, 1f);
    }
}