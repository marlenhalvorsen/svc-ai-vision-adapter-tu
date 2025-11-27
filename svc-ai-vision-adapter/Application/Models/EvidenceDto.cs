namespace svc_ai_vision_adapter.Application.Models
{
    public sealed record EvidenceDto(
        string? WebBestGuess,
        string? Logo,
        string? OcrSample,
        IReadOnlyList<WebEntityHitDto>? WebEntities,
        IReadOnlyList<LogoHitDto>? LogoCandidates
    );
}
