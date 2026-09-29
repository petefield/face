using Microsoft.Extensions.Hosting;

public sealed class RobotRendererService : BackgroundService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly FaceState faceState;

    public RobotRendererService(
        IHostApplicationLifetime applicationLifetime,
        FaceState faceState)
    {
        _applicationLifetime = applicationLifetime;
        this.faceState = faceState;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            using var renderer = new RobotRenderer();
            renderer.Start(stoppingToken, faceState);

            // If the window is closed manually, stop the web host as well.
            if (!stoppingToken.IsCancellationRequested)
            {
                _applicationLifetime.StopApplication();
            }
        }, stoppingToken);
    }
}
