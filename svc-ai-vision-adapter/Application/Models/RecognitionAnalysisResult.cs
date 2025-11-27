using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Transport;

namespace svc_ai_vision_adapter.Application.Models
{
    public sealed record RecognitionAnalysisResult
    {
        public AIProviderDto Provider { get; init; } = default!;
        public InvocationMetricsDto InvocationMetrics { get; init; } = default!;
        public IReadOnlyList<ProviderResultDto> Results { get; init; } = Array.Empty<ProviderResultDto>();

    }

}
