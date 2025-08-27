using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;

namespace svc_ai_vision_adapter.Application.Services
{
    internal sealed class RecognitionService(IAnalyzerFactory factory, IImageFetcher fetcher) : IRecognitionService
    {
        public async Task<RecognitionResponseDto> AnalyzeAsync(RecognitionRequestDto req, CancellationToken ct = default)
        {
            var features = (req.Features?.Count > 0 ? req.Features : new List<string>() { "LABEL_DETECTION", "LOGO_DETECTION"})
                            .Select(s=> s.Trim().ToUpperInvariant())
                            .ToList();

            var analyzer = factory.Resolve(req.Provider);

            var images = await Task.WhenAll(req.Images.Select(i => fetcher.FetchAsync(i, ct)));

            var (ai, metrics, results) = await analyzer.AnalyzeAsync(images, features, ct);

            return new RecognitionResponseDto(req.sessionId, ai, metrics, results.ToList());
        }
    }
}
