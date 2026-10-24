using Microsoft.Extensions.Hosting;

public sealed class RobotRendererService(
    IHostApplicationLifetime applicationLifetime,
    FaceState faceState) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() =>
        {
            using var renderer = new RobotRenderer();
            renderer.Start(stoppingToken, faceState);

            // If the window is closed manually, stop the web host as well.
            if (!stoppingToken.IsCancellationRequested)
                applicationLifetime.StopApplication();
        }, stoppingToken);
}
