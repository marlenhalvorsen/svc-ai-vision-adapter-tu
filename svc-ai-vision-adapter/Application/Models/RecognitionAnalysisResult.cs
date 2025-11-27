using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Transport;

namespace svc_ai_vision_adapter.Application.Models
{
    /// <summary>
    /// represents internal model of the returned payload from Google Visions analysis.
    /// </summary>
    public sealed record RecognitionAnalysisResult
    {
        public AIProviderDto Provider { get; init; } = default!;
        public InvocationMetricsDto InvocationMetrics { get; init; } = default!;
        public IReadOnlyList<ProviderResultDto> Results { get; init; } = Array.Empty<ProviderResultDto>();

    }

}
