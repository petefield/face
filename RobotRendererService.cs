using Microsoft.Extensions.Hosting;

public sealed class RobotRendererService : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly EmotionState _emotionState;

    public RobotRendererService(
        IHostApplicationLifetime applicationLifetime,
        EmotionState emotionState)
    {
        _applicationLifetime = applicationLifetime;
        _emotionState = emotionState;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            using var renderer = new RobotRenderer();
            renderer.Start(stoppingToken, _emotionState);

            // If the window is closed manually, stop the web host as well.
            if (!stoppingToken.IsCancellationRequested)
            {
                _applicationLifetime.StopApplication();
            }
        }, stoppingToken);
    }
}
