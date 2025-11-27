using svc_ai_vision_adapter.Application.Contracts.Transport;
using System.Text.Json;

namespace svc_ai_vision_adapter.Application.Contracts
{

    /// <summary>
    /// We map provider Software Develomplent Kit (SDK) into our own DTO's. 
    /// By doing this we ensure our public API is stable and provider-agnostic,
    /// so clients do not depend on Google/AWS/Azure SDK types directly. 
    /// </summary>


    /// <summary> 
    /// Describes what provider was used to be transparent and audit without exposing SDK-details.
    /// </summary>
    public sealed record AIProviderDto(
        string Name, 
        string? ApiVersion, 
        IReadOnlyList<string> Featureset,
        int? MaxResults = null,
        string? ReasoningName = null,
        string? ReasoningModel = null);

    /// <summary>
    /// provides visibility that can be used for performance monitoring and troubleshooting
    /// </summary>
    public sealed record InvocationMetricsDto(
        int LatencyMs, 
        int ImageCount, 
        string? ProviderRequestId);

    /// <summary>
    /// Ref for image and JSON element. "RAW" is kept as an object (JSON element) so it can be debugged or used if needed. 
    /// </summary>
    public sealed record ProviderResultDto(
        ImageRefDto ImageRef, 
        JsonElement Raw);

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

    public sealed record EvidenceDto(
        string? WebBestGuess,
        string? Logo,
        string? OcrSample,
        IReadOnlyList<WebEntityHitDto>? WebEntities,
        IReadOnlyList<LogoHitDto>? LogoCandidates
    );

    public sealed record WebEntityHitDto(
        string Description,
        double Score
    );
    public sealed record LogoHitDto(
        string Description, 
        double Score
        );
    public sealed record MachineAggregateDto
    {
        public string? Brand { get; init; }
        public string? MachineType { get; init; }
        public string? Model { get; init; }
        public double? Weight { get; init; }
        public string? Year { get; init; }
        public List<string>? Attachment { get; init; }

        public double Confidence { get; init; }
        public bool IsConfident { get; init; }
        public double? TypeConfidence { get; init; }
        public string? TypeSource { get; init; }
        
        public string Name => string.Join(", ", new[] { Brand, MachineType, Model  }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim()));
    }

    public sealed record ShapedResultDto(
        ImageRefDto ImageRef,
        MachineSummaryDto Machine,
        EvidenceDto Evidence
    );
    public sealed record RecognitionResponseDto(
        AIProviderDto Ai,
        InvocationMetricsDto Metrics,
        List<ProviderResultDto> Results,
        string? CorrelationId,
        string ObjectKey,
        List<ShapedResultDto>? Compact = null,
        MachineAggregateDto? Aggregate = null
        );
}
