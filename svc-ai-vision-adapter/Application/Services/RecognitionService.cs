using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Inbound;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Application.Transport;
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
    /// 4. Builds RecognitionResponseDto.
    /// 5. Enrichs metadata with LLM response if EnableReasoning in options are true
    /// </summary>
    internal sealed class RecognitionService : IRecognitionService
    {
        private readonly IImageFetcher _fetcher;
        private readonly RecognitionOptions _opt;
        private readonly IResultAggregator _aggregator;
        private readonly IResultShaper _shaper;
        private readonly IImageUrlFetcher _urlFetcher;
        private readonly IImageAnalyzer _imageAnalyzer;
        private readonly IMachineReasoningAnalyzer _machineReasoning;
        private readonly IReasoningProviderInfo _reasoningProviderInfo;



        public RecognitionService(
            IImageUrlFetcher imageUrlFetcher,
            IImageFetcher fetcher,
            IOptions<RecognitionOptions> opt,
            IImageAnalyzer imageAnalyzer,
            IResultShaper shaper,
            IResultAggregator aggregator,
            IMachineReasoningAnalyzer machineReasoning,
            IReasoningProviderInfo reasoningProviderInfo)
        {
            _urlFetcher = imageUrlFetcher;
            _fetcher = fetcher;
            _opt = opt.Value;
            _imageAnalyzer = imageAnalyzer;
            _aggregator = aggregator;
            _shaper = shaper;
            _machineReasoning = machineReasoning;
            _reasoningProviderInfo = reasoningProviderInfo;
        }

        public async Task<RecognitionResponseDto> AnalyzeAsync(
            MessageKey request,
            CancellationToken ct = default)
        {
            //fetch presigned urls
            var presignedUrls = await _urlFetcher.FetchUrlAsync(request.ObjectKey, ct);

            //fetch images via presigned urls
            var url = await _urlFetcher.FetchUrlAsync(request.ObjectKey, ct);
            var image = await _fetcher.FetchAsync(new ImageRefDto(url), ct);
            var images = new List<(ImageRefDto Ref, byte[] Bytes)> { image };

            //find server configured features
            var features = _opt.Features ?? new List<string>();

            //analyse images
            var result = await _imageAnalyzer
                .AnalyzeAsync(images, features, ct);

            //shape result 
            var compact = result
                .Results
                .Select(_shaper.Shape)
                .ToList(); //shapes each result from the list to shapedResult

            //aggregate the shaped result 
            var aggregate = _aggregator
                .Aggregate(compact); //aggregate compact results

            //builds RecognitionResponseDto
            var response = new RecognitionResponseDto(
                Ai: result.Provider,
                Metrics: result.InvocationMetrics,
                Results: _opt.IncludeRaw ? result.Results.ToList() : new List<ProviderResultDto>(),
                CorrelationId: request.CorrelationId,
                ObjectKey: request.ObjectKey,
                Compact: compact,
                Aggregate: aggregate
                );


            //enable machineReasoning if true in appsettings
            if (_opt.EnableReasoning)
            {
                var refined = await _machineReasoning.AnalyzeAsync(aggregate, ct);

                //adding providerInfo
                var provider = new AIProviderDto(
                    Name: _reasoningProviderInfo.Name,           
                    ApiVersion: _reasoningProviderInfo.Model,   
                    Featureset: new List<string> { "machine_reasoning" },
                    MaxResults: null
                );

                // update AI metadata with reasoning information
                response = response with
                {
                    Aggregate = refined,
                    Ai = response.Ai with
                    {
                        ReasoningName = provider.Name,
                        ReasoningModel = provider.ApiVersion
                    }
                };

                return response;
            }

            return response;

        }
    }
}
