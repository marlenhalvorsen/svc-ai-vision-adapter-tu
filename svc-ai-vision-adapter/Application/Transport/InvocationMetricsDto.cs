namespace svc_ai_vision_adapter.Application.Transport
{
    /// <summary>
    /// provides visibility that can be used for performance monitoring and troubleshooting
    /// </summary>
    public sealed record InvocationMetricsDto(
        int LatencyMs,
        int ImageCount,
        string? ProviderRequestId);
}
