using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.In;
using svc_ai_vision_adapter.Application.Ports.Out;
using svc_ai_vision_adapter.Application.Services.Factories;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Producers;
using svc_ai_vision_adapter.Infrastructure.Options;

namespace svc_ai_vision_adapter.Application.Services
{

    /// <summary>
    /// Main service for recognition Use case. 
    /// Orchestrator calls this via controller. 
    /// Service: 
    /// 1. What features to be applied
    /// 2. fetch bytes via IImageFetcher
    /// 3. sends pictures and features to analyzer. 
    /// 4. Returns RecognitionResponseDto.
    /// </summary>
    internal sealed class RecognitionService : IRecognitionService
    {
        private readonly IAnalyzerFactory _factory;
        private readonly IImageFetcher _fetcher;
        private readonly RecognitionOptions _opt;
        private readonly IResultAggregator _aggregator;
        private readonly IResultShaper _shaper;
        private readonly IRecognitionCompletedPublisher _publisher;

        private static readonly HashSet<string> FeatureAllowList = new(StringComparer.OrdinalIgnoreCase)
        {
            "LabelDetection","LogoDetection","DocumentTextDetection","TextDetection",
            "ObjectLocalization","WebDetection"
        };

        private static readonly List<string> DefaultFeatures = new()
        {
            "LogoDetection", "DocumentTextDetection", "WebDetection"
        };

        public RecognitionService(
            IAnalyzerFactory factory, 
            IImageFetcher fetcher, 
            IOptions<RecognitionOptions> opt, 
            IResultShaper shaper, 
            IResultAggregator aggregator, IRecognitionCompletedPublisher publisher)
        {
            _factory = factory;
            _fetcher = fetcher;
            _opt = opt.Value;
            _aggregator = aggregator;
            _shaper = shaper;
            _publisher = publisher;
        }

        public async Task<RecognitionResponseDto> AnalyzeAsync(
            RecognitionRequestDto req, 
            CancellationToken ct = default)
        {
            // Use server configured features 
            var configured = _opt.Features?.Count > 0 ? _opt.Features : DefaultFeatures;
            var features = configured
                .Where(f => FeatureAllowList.Contains(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var analyzer = _factory.Resolve(req.Provider);
            var images = await Task
                .WhenAll(req.Images
                .Select(i => _fetcher.FetchAsync(i, ct)));

            var result = await analyzer
                .AnalyzeAsync(images, features, ct);

            var compact = result
                .Results
                .Select(_shaper.Shape)
                .ToList(); //shapes each result from the list to shapedResult
            var aggregate = _aggregator
                .Aggregate(compact); //aggregate compact results

            var response = new RecognitionResponseDto(
                SessionId: req.SessionId,
                Ai: result.Provider,
                Metrics: result.InvocationMetrics,
                Results: _opt.IncludeRaw ? result.Results.ToList() : new List<ProviderResultDto>(),
                Compact: compact,
                Aggregate: aggregate);

            return response; 
        }
    }
}
