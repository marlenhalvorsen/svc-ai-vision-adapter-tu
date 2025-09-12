namespace svc_ai_vision_adapter.Application.Contracts
{
    public sealed record RecognitionAnalysisResult(
        AIProviderDto Provider,
        InvocationMetricsDto InvocationMetrics,
        IReadOnlyList<ProviderResultDto> Results);
}
