namespace svc_ai_vision_adapter.Application.Models
{
    /// <summary>
    /// represents the result from output Google Vision provides in feature "LogoDetection"
    /// </summary>
    /// <param name="Description"></param>
    /// <param name="Score"></param>
    public sealed record LogoHitDto(
        string Description,
        double Score
        );
}
