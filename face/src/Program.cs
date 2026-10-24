var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidation();
builder.Services.AddSingleton<FaceState>();
builder.Services.AddSingleton<ServoController>();
builder.Services.AddSingleton<PoseService>();
builder.Services.AddHostedService<RobotRendererService>();

var app = builder.Build();

app.MapEndpoints();

app.Run();
