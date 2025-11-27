namespace svc_ai_vision_adapter.Application.Models
{
    /// <summary>
    /// summary fo the evidence Google Vision has sampled. 
    /// </summary>

    public sealed record EvidenceDto(
        string? WebBestGuess,
        string? Logo,
        string? OcrSample,
        IReadOnlyList<WebEntityHitDto>? WebEntities,
        IReadOnlyList<LogoHitDto>? LogoCandidates
    );
}
