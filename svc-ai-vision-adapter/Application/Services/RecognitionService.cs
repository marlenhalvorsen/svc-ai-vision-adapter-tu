﻿using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;
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

        private static readonly HashSet<string> FeatureAllowList = new(StringComparer.OrdinalIgnoreCase)
        {
            "LabelDetection","LogoDetection","DocumentTextDetection","TextDetection",
            "ObjectLocalization","WebDetection"
        };

        private static readonly List<string> DefaultFeatures = new()
        {
            "LabelDetection","LogoDetection"
        };

        public RecognitionService(IAnalyzerFactory factory, IImageFetcher fetcher, IOptions<RecognitionOptions> opt)
        {
            _factory = factory;
            _fetcher = fetcher;
            _opt = opt.Value;
        }

        public async Task<RecognitionResponseDto> AnalyzeAsync(RecognitionRequestDto req, CancellationToken ct = default)
        {
            // Use server configured features (ignore req.Features)
            var configured = _opt.Features?.Count > 0 ? _opt.Features : DefaultFeatures;
            var features = configured
                .Where(f => FeatureAllowList.Contains(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var analyzer = _factory.Resolve(req.Provider);
            var images = await Task.WhenAll(req.Images.Select(i => _fetcher.FetchAsync(i, ct)));
            var result = await analyzer.AnalyzeAsync(images, features, ct);
            var ai = result.provider;
            var metrics = result.invocationMetrics;
            var results = result.results;
            
            return new RecognitionResponseDto(req.sessionId, ai, metrics, results.ToList());
        }
    }
}
