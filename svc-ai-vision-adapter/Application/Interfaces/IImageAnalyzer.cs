using svc_ai_vision_adapter.Application.Contracts;

namespace svc_ai_vision_adapter.Application.Interfaces
{
    /// <summary>
    /// Port towards the vision-APIs, implemented by adapteres (Google/AWS/Mock).
    /// </summary>
    public interface IImageAnalyzer
    {
        Task<(AIProviderDto provider,
              InvocationMetricsDto invocationMetrics,
              IReadOnlyList<ProviderResultDto> results)>
        AnalyzeAsync(
            IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)> images,
            IReadOnlyList<string> features,
            CancellationToken ct = default); //CT impemented to be able to cancel operation if not needed anymore. 
    }
}
