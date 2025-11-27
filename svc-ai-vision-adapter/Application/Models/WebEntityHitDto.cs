namespace svc_ai_vision_adapter.Application.Models
{
    /// <summary>
    /// represents the result from output Google Vision provides in feature "WebDetection"
    /// </summary>
    /// <param name="Description"></param>
    /// <param name="Score"></param>
    public sealed record WebEntityHitDto(
        string Description,
        double Score
    );
}
