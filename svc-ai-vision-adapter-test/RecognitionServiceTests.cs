using System.Text.Json;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;
using svc_ai_vision_adapter.Application.Services;
using FluentAssertions;
using Xunit;

public class RecognitionServiceTests
{
    // Tiny factory that always returns the same analyzer
    private sealed class TestFactory(IImageAnalyzer a) : IAnalyzerFactory
    { public IImageAnalyzer Resolve(string? _) => a; }

    // Fake analyzer that records what it got
    private sealed class FakeAnalyzer : IImageAnalyzer
    {
        public IReadOnlyList<string>? ReceivedFeatures { get; private set; }

        // >>> Match interfacets præcise tuple-navne: provider, invocationMetrics, results
        public Task<(AIProviderDto provider, InvocationMetricsDto invocationMetrics, IReadOnlyList<ProviderResultDto> results)>
            AnalyzeAsync(
                IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)> images,
                IReadOnlyList<string> features,
                CancellationToken ct = default)
        {
            ReceivedFeatures = features.ToList();

            var results = images
                .Select(i => new ProviderResultDto(i.Ref, JsonDocument.Parse("""{"ok":true}""").RootElement))
                .ToList();

            var provider = new AIProviderDto("fake", "v1", null, features, new { });
            var invocationMetrics = new InvocationMetricsDto(10, images.Count, null);

            // Navngiv return-tuple elementerne eksplicit, så de matcher interfacet
            return Task.FromResult(
                (provider: provider,
                 invocationMetrics: invocationMetrics,
                 results: (IReadOnlyList<ProviderResultDto>)results)
            );
        }
    }

    private sealed class FakeFetcher : IImageFetcher
    {
        public Task<(ImageRefDto Ref, byte[] Bytes)> FetchAsync(ImageRefDto img, CancellationToken ct = default)
            => Task.FromResult((img, new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task Defaults_to_Label_And_Logo_When_Features_Missing()
    {
        var analyzer = new FakeAnalyzer();

        // Brug interfacet, hvis RecognitionService implementerer AnalyzeAsync eksplicit
        IRecognitionService svc = new RecognitionService(new TestFactory(analyzer), new FakeFetcher());

        var req = new RecognitionRequestDto(
            "s1",
            new() { new ImageRefDto("https://example/img.png") },
            Features: null,
            Provider: null
        );

        var resp = await svc.AnalyzeAsync(req, CancellationToken.None);

        analyzer.ReceivedFeatures.Should().BeEquivalentTo(new[] { "LABEL_DETECTION", "LOGO_DETECTION" });
        resp.Results.Should().HaveCount(1);
        resp.Ai.Name.Should().Be("fake");
    }
}
