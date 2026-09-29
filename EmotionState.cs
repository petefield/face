using System.Threading;

public sealed class FaceState
{
    private string _emotion = "neutral";

    public string Emotion
    {
        get => Volatile.Read(ref _emotion);
        set => Volatile.Write(ref _emotion, value);
    }
}
