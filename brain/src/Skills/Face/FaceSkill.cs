using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Brain.Skills;

/// <summary>Controls the robot's face expression.</summary>
public sealed class FaceSkill : ISkill
{
    private readonly IFaceClient _faceClient;
    private readonly ILogger<FaceSkill> _logger;

    public FaceSkill( IFaceClient faceClient, ILogger<FaceSkill> logger)
    {
        _logger = logger;
        _faceClient = faceClient;

 }

    public string Name => "control_face";

    public string Description => "Controls the robot's face expression.";

    public BinaryData ParameterSchema => SkillSchema.Create<FaceArguments>();

    public async Task<string> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryParse<FaceArguments>(out var parsed))
        {
            return "Failed: 'state' is required.";
        }

         _logger.LogInformation("Setting robot face {State}.", parsed.State);

        try
        {
            await _faceClient.SetEmotionAsync(parsed.State.ToString(), cancellationToken);
            Console.WriteLine("Setting face to {0}", parsed.State);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Failed to set robot face to {State}.", parsed.State.ToString());
        }
    
        return $"The face is now {parsed.State.ToString()}";
    }
}
