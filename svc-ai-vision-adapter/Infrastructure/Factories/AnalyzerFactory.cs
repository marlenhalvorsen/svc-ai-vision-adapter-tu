using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Infrastructure.Options;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision;
using svc_ai_vision_adapter.Application.Ports.Out;
using svc_ai_vision_adapter.Application.Services.Factories;

namespace svc_ai_vision_adapter.Infrastructure.Factories
{
    internal sealed class AnalyzerFactory : IAnalyzerFactory
    {
        private readonly IServiceProvider _sp;
        private readonly RecognitionOptions _opt;

        public AnalyzerFactory(IServiceProvider sp, IOptions<RecognitionOptions> opt)
        {
            _sp = sp;
            _opt = opt.Value;
        }

        public IImageAnalyzer Resolve(string? providerKey)
        {
            var key = (providerKey ?? _opt.DefaultProvider).ToLowerInvariant();
            return key switch
            {
                "google" => _sp.GetRequiredService<GoogleVisionAnalyzer>(),
                _ => _sp.GetRequiredService<GoogleVisionAnalyzer>() //deafult value
            };

        }
    }
}
