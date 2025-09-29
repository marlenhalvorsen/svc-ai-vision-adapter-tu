//using System.Text.Json;
//using Microsoft.Extensions.Options;
//using svc_ai_vision_adapter.Application.Contracts;
//using svc_ai_vision_adapter.Application.Interfaces;
//using svc_ai_vision_adapter.Application.Services;
//using svc_ai_vision_adapter.Infrastructure.Options;
//using FluentAssertions;
//using Xunit;

//public class RecognitionServiceTests
//{
//    // ARRANGE
//    // Tiny factory that always returns the same analyzer
//    private sealed class TestFactory(IImageAnalyzer a) : IAnalyzerFactory
//    { public IImageAnalyzer Resolve(string? _) => a; }

//    // Fake analyzer that records what it got 
//    private sealed class FakeAnalyzer : IImageAnalyzer
//    {
//        public IReadOnlyList<string>? ReceivedFeatures { get; private set; }

//        public Task<RecognitionAnalysisResult> AnalyzeAsync(
//            IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)> images,
//            IReadOnlyList<string> features,
//            CancellationToken ct = default)
//        {
//            ReceivedFeatures = features.ToList();

//            var rawResults = images
//                .Select(i => new ProviderResultDto(i.Ref, JsonDocument.Parse("""{"ok":true}""").RootElement))
//                .ToList();

//            var provider = new AIProviderDto("fake", "v1", null, features, new { });
//            var invocationMetrics = new InvocationMetricsDto(10, images.Count, null);

//            // Normaliseret resultat – indholdet er ikke vigtigt for denne test
//            var normalized = images
//                .Select(i => new NormalizedResult(
//                    i.Ref,
//                    Labels: new[] { "ok" },
//                    Logo: null,
//                    OcrText: null,
//                    Objects: Array.Empty<(string, double)>(),
//                    WebBestGuess: null))
//                .ToList();

//            return Task.FromResult(new RecognitionAnalysisResult(
//                Provider: provider,
//                InvocationMetrics: invocationMetrics,
//                Results: rawResults
//            ));
//        }
//    }

//    private sealed class FakeFetcher : IImageFetcher
//    {
//        public Task<(ImageRefDto Ref, byte[] Bytes)> FetchAsync(ImageRefDto img, CancellationToken ct = default)
//            => Task.FromResult((img, new byte[] { 1, 2, 3 }));
//    }

//    [Fact]
//    public async Task Defaults_to_Label_And_Logo_When_Features_Missing()
//    {
//        var analyzer = new FakeAnalyzer();

//        // Service kræver IOptions<RecognitionOptions>; giv ingen features => brug defaults
//        var opts = Options.Create(new RecognitionOptions
//        {
//            Features = null // eller: new List<string>()
//        });

//        IRecognitionService svc = new RecognitionService(new TestFactory(analyzer), new FakeFetcher(), opts);

//        var req = new RecognitionRequestDto(
//            "s1",
//            new() { new ImageRefDto("https://example/img.png") },
//            Features: null,
//            Provider: null
//        );

//        var resp = await svc.AnalyzeAsync(req, CancellationToken.None);

//        // Service bruger camelCase defaults
//        analyzer.ReceivedFeatures.Should().BeEquivalentTo(new[] { "LabelDetection", "LogoDetection" });

//        resp.Results.Should().HaveCount(1);
//        resp.Ai.Name.Should().Be("fake");
//    }
//}
