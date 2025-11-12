using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Inbound;
using svc_ai_vision_adapter.Application.Ports.Outbound;
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
        private readonly IImageFetcher _fetcher;
        private readonly RecognitionOptions _opt;
        private readonly IResultAggregator _aggregator;
        private readonly IResultShaper _shaper;
        private readonly IImageUrlFetcher _urlFetcher;
        private readonly IImageAnalyzer _imageAnalyzer;


        public RecognitionService(
            IImageUrlFetcher imageUrlFetcher, 
            IImageFetcher fetcher, 
            IOptions<RecognitionOptions> opt, 
            IImageAnalyzer imageAnalyzer,
            IResultShaper shaper, 
            IResultAggregator aggregator)
        {
            _urlFetcher = imageUrlFetcher;
            _fetcher = fetcher;
            _opt = opt.Value;
            _imageAnalyzer = imageAnalyzer;
            _aggregator = aggregator;
            _shaper = shaper;
        }

        public async Task<RecognitionResponseDto> AnalyzeAsync(
            MessageKey request, 
            CancellationToken ct = default)
        {
            //fech presigned urls
            var presignedUrls = await Task.WhenAll(
                request.ObjectKeys.Select(k => _urlFetcher.FetchUrlAsync(k, ct))
            );

            //fetch images via presigned urls
            var images = await Task.WhenAll(
                presignedUrls.Select(url => _fetcher.FetchAsync(new ImageRefDto(url), ct))
            );

            //find server configured features
            var features = _opt.Features ?? new List<string>();

            //analyse images
            var result = await _imageAnalyzer
                .AnalyzeAsync(images, features, ct);

            //shape and aggregate
            var compact = result
                .Results
                .Select(_shaper.Shape)
                .ToList(); //shapes each result from the list to shapedResult
            var aggregate = _aggregator
                .Aggregate(compact); //aggregate compact results

            var response = new RecognitionResponseDto(
                Ai: result.Provider,
                Metrics: result.InvocationMetrics,
                Results: _opt.IncludeRaw ? result.Results.ToList() : new List<ProviderResultDto>(),
                CorrelationId: request.CorrelationId,
                ObjectKeys: request.ObjectKeys,
                Compact: compact,
                Aggregate: aggregate
                );

            return response; 
        }
    }
}
