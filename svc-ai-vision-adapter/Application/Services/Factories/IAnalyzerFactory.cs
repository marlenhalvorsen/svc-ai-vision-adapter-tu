using svc_ai_vision_adapter.Application.Ports.Out;

namespace svc_ai_vision_adapter.Application.Services.Factories
{
    /// <summary>
    /// Routing abstraction for choosing the correct vision-adapter (Google/AWS/Mock)
    /// based on providerKey or configuration.
    /// </summary>
    public interface IAnalyzerFactory
    {
        IImageAnalyzer Resolve(string? providerKey);
    }
}
