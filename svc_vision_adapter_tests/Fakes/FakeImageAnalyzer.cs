using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Models;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using System.Text.Json;

/// <summary>
/// Fake implementation of <see cref="IImageAnalyzer"/> used for testing the recognition flow.
/// Loads a static JSON response from test data to simulate an AI provider (e.g., Google Vision).
/// Returns deterministic results to allow end-to-end tests without external dependencies.
/// </summary>
namespace svc_vision_adapter_tests.Fakes
{
    internal sealed class FakeImageAnalyzer : IImageAnalyzer
    {
        public Task<RecognitionAnalysisResult> AnalyzeAsync(
            IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)> images, 
            IReadOnlyList<string> features, 
            CancellationToken ct)
        {
            // Find testdatafile with real data from in-memory JSON response from google vision
            var basePath = AppContext.BaseDirectory;
            var path = Path.Combine(basePath, "tests", "Infrastructure", "Adapters", "VisionAdapterTest", "TestData", "GoogleVisionSample.json");

            if (!File.Exists(path))
                throw new FileNotFoundException($"Testdata not found at path: {path}");

            var json = File.ReadAllText(path);
            var raw = JsonDocument.Parse(json).RootElement;

            // providerResult pr image
            var results = images
                .Select(img => new ProviderResultDto(
                    new ImageRefDto(img.Ref.Url),
                    raw
                ))
                .ToList();

            // Return result
            var fakeResult = new RecognitionAnalysisResult
            {
                Provider = new AIProviderDto(
                    Name: "FakeVision",
                    ApiVersion: "vTest",
                    Featureset: features.ToList()
                ),
                InvocationMetrics = new InvocationMetricsDto(
                    LatencyMs: 42,
                    ImageCount: results.Count,
                    ProviderRequestId: Guid.NewGuid().ToString()
                ),
                Results = results
            };

            return Task.FromResult(fakeResult);
        }
    }
}
