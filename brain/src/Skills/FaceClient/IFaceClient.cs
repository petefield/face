namespace Brain;

public interface IFaceClient
{
    Task SetEmotionAsync(string state, CancellationToken cancellationToken = default);
    Task<bool> LookAsync(int x, int y, CancellationToken cancellationToken = default);
}