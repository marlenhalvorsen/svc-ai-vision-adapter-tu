using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;
using svc_ai_vision_adapter.Infrastructure.Options;
using Google.Cloud.Vision.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision
{
    internal sealed class GoogleVisionAnalyzer : IImageAnalyzer
    {
        private readonly  ImageAnnotatorClient _imageAnnotatorClient;
        private readonly RecognitionOptions _recognitionOptions;

        public GoogleVisionAnalyzer(IOptions<RecognitionOptions> opt)
        {
            _recognitionOptions = opt.Value;
            _imageAnnotatorClient = ImageAnnotatorClient.Create();
        }

        public async Task<RecognitionAnalysisResult> AnalyzeAsync(
            IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)> images, IReadOnlyList<string> features, CancellationToken ct = default)
        {
            var batch = new BatchAnnotateImagesRequest();

            foreach (var image in images)
            {
                var req = new AnnotateImageRequest { Image = Image.FromBytes(image.Bytes) };

                foreach (var feature in features)
                    if (Enum.TryParse<Feature.Types.Type>(feature, true, out var t))
                        req.Features.Add(new Feature { Type = t, MaxResults = _recognitionOptions.MaxResults });

                batch.Requests.Add(req);
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var resp = await _imageAnnotatorClient.BatchAnnotateImagesAsync(batch, ct);
            var latency = (int)sw.Elapsed.TotalMilliseconds;

            var jsonFmt = JsonFormatter.Default;
            var results = new List<ProviderResultDto>();
            for (int i = 0; i < resp.Responses.Count; i++)
            {
                var raw = JsonDocument.Parse(jsonFmt.Format(resp.Responses[i])).RootElement;
                results.Add(new ProviderResultDto(images[i].Ref, raw));
            }

            var ai = new AIProviderDto("vision", "v1", _recognitionOptions.Region, features, new { _recognitionOptions.MaxResults });
            var metrics = new InvocationMetricsDto(latency, images.Count, ProviderRequestId: null);

            return new RecognitionAnalysisResult(ai, metrics, results); 
        }
    }
}
