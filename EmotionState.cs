using System.Threading;

public sealed class EmotionState
{
    private string _emotion = "Neutral";

    public string Emotion
    {
        get => Volatile.Read(ref _emotion);
        set => Volatile.Write(ref _emotion, value);
    }
}
