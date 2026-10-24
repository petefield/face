using System.Numerics;

public static class Emotions
{
    public static readonly string[] All = ["happy", "angry", "neutral", "sleep"];
}

public sealed class FaceState
{
    private string _emotion = "neutral";
    private Vector2 _lookDirection = new Vector2(-310, +240);

    public string Emotion
    {
        get => Volatile.Read(ref _emotion);
        set => Volatile.Write(ref _emotion, value);
    }

    public bool TryApplyExpression(string value)
    {
        Volatile.Write(ref _emotion, value);
        return true;
    }


    public Vector2 LookDirection
    {
        get =>  _lookDirection;
        set => _lookDirection = value;
    }
}
