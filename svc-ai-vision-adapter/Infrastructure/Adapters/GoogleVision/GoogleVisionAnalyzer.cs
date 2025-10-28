using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Options;
using Google.Cloud.Vision.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using System.Text.Json;
using svc_ai_vision_adapter.Application.Services;
using Google.Api;
using svc_ai_vision_adapter.Application.Ports.Out;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision
{
    internal sealed class GoogleVisionAnalyzer : IImageAnalyzer
    {
        private readonly ImageAnnotatorClient _imageAnnotatorClient;
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



            var ai = new AIProviderDto(
           Name: "vision",
           ApiVersion: "v1",
           Region: _recognitionOptions.Region,
           Featureset: features.ToList(),
           Config: new { MaxResults = _recognitionOptions.MaxResults }
       );

            var metrics = new InvocationMetricsDto(
            LatencyMs: latency,
            ImageCount: images.Count,
            ProviderRequestId: null
        );
            return new RecognitionAnalysisResult
            {
                Provider = ai,
                InvocationMetrics = metrics,
                Results = _recognitionOptions.IncludeRaw ? results : Array.Empty<ProviderResultDto>(),
            };
        }
    }
}
