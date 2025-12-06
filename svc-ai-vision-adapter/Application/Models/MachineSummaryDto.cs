namespace svc_ai_vision_adapter.Application.Models
{
    // <summary>
    /// Compact/shaped result per image (derived from Raw).
    /// </summary>
    public sealed record MachineSummaryDto(
        string? Type,
        string? Brand,
        string? Model,
        double Confidence,
        bool IsConfident
    );

}
