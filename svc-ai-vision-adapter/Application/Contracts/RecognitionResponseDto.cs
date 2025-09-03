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
    public record AIProviderDto(string Name, string ApiVersion, string? Region, IReadOnlyList<string> Featureset, object? Conifg);
    /// <summary>
    /// provides visibility that can be used for performance monitoring and troubleshooting
    /// </summary>
    public record InvocationMetricsDto(int LatencyMs, int ImageCount, string? ProviderRequestId);
    /// <summary>
    /// Ref for image and JSON element. "RAW" is kept as an object (JSON element) so it can be debugged or used if needed. 
    /// </summary>
    public record ProviderResultDto(ImageRefDto ImageRef, JsonElement Raw);
    public record RecognitionResponseDto(
        string? SessionId,
        AIProviderDto Ai,
        InvocationMetricsDto Metrics,
        List<ProviderResultDto> Results
        );
}
