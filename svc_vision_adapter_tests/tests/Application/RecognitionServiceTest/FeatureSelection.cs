using Microsoft.Extensions.Options;
using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Out;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Application.Services.Factories;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Infrastructure.Options;
using System.Text.Json;
using Xunit.Sdk;

namespace svc_vision_adapter_tests.Application.RecognitionServiceTest;

[TestClass]
public class FeatureSelection
{
    [TestMethod]
    public async Task Uses_Default_Features_When_Config_Empty()
    {
        // ARRANGE 
        var factory = new Mock<IAnalyzerFactory>();
        var fetcher = new Mock<IImageFetcher>();
        var analyzer = new Mock<IImageAnalyzer>();
        var shaper = Mock.Of<IResultShaper>();
        var aggregator = Mock.Of<IResultAggregator>();
        var publisher = Mock.Of<IRecognitionCompletedPublisher>();


        // Return fake bytes when fetching image
        fetcher.Setup(f => f.FetchAsync(
            It.IsAny<ImageRefDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new ImageRefDto("img-001"), new byte[] { 1, 2, 3 }));

        // Factory returns our mock analyzer
        factory
            .Setup(f => f.Resolve(It.IsAny<string>()))
            .Returns(analyzer.Object);

        var rawJson = JsonDocument.Parse("{\"data\":\"some raw result\"}").RootElement;

        // Analyzer returns a fake result
        analyzer.Setup(a => a.AnalyzeAsync(
                It.IsAny<IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecognitionAnalysisResult
            {
                Provider = new AIProviderDto(
                    Name: "Google Vision",
                    ApiVersion: null,
                    Region: null,
                    Featureset: null,
                    Config: null),
                InvocationMetrics = new InvocationMetricsDto(
                    LatencyMs: 0,
                    ImageCount: 1,
                    ProviderRequestId: "123"),
                Results = new List<ProviderResultDto>
                {
                    new ProviderResultDto
                    (
                        new ImageRefDto("img-001"), rawJson
                    )
                }
            });


        var options = Options.Create(new RecognitionOptions 
        { 
            Features = new List<string>() 
        });
        var service = new RecognitionService(factory.Object, fetcher.Object, options, shaper, aggregator, publisher);
        var request = new RecognitionRequestDto(
            "123",
            new List<ImageRefDto> 
            { 
                new("img-001") 
            },
            "Google Vision"
        );

        // ACT
        await service.AnalyzeAsync(request, CancellationToken.None);

        // ASSERT
        analyzer.Verify(a => a.AnalyzeAsync(
                It.IsAny<IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)>>(),
                It.Is<IReadOnlyList<string>>(
                    features => features.SequenceEqual(
                        new[] 
                        { 
                            "LabelDetection", 
                            "LogoDetection",
                            "DocumentTextDetection"
                        })
                ),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

}
