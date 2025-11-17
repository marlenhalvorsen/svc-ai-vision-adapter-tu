using Microsoft.Extensions.Options;
using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Application.Models;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Infrastructure.Options;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Application.Models;


namespace svc_vision_adapter_tests.Application.RecognitionServiceTest;

[TestClass]
public class FeatureSelection
{
    [TestMethod]
    public async Task Uses_Default_Features()
    {
        // ARRANGE
        var urlFetcher = new Mock<IImageUrlFetcher>();
        var analyzer = new Mock<IImageAnalyzer>();
        var fetcher = new Mock<IImageFetcher>();
        var shaper = Mock.Of<IResultShaper>();
        var aggregator = Mock.Of<IResultAggregator>();
        var publisher = Mock.Of<IRecognitionCompletedPublisher>();

        // fake image bytes
        fetcher.Setup(f => f.FetchAsync(
            It.IsAny<ImageRefDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new ImageRefDto("img-001"), new byte[] { 1, 2, 3 }));

        // fake analyzer result
        var rawJson = JsonDocument.Parse("{\"data\":\"some raw result\"}").RootElement;
        analyzer.Setup(a => a.AnalyzeAsync(
                It.IsAny<IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecognitionAnalysisResult
            {
                Provider = new AIProviderDto("Google Vision", null, null, null),
                InvocationMetrics = new InvocationMetricsDto(0, 1, "123"),
                Results = new List<ProviderResultDto>
                {
                    new ProviderResultDto(new ImageRefDto("img-001"), rawJson)
                }
            });

        var options = Options.Create(new RecognitionOptions { Features = new List<string>() });

        // hvis din RecognitionService tager GoogleVisionAnalyzer direkte
        var service = new RecognitionService(
            urlFetcher.Object,
            fetcher.Object,
            options,
            analyzer.Object,
            shaper,
            aggregator);

        var messageKey = new MessageKey(new List<string> { "img-001" }, "corr-123");

        // ACT
        await service.AnalyzeAsync(messageKey, CancellationToken.None);

        // ASSERT – tjek at analyzer fik en tom liste
        analyzer.Verify(a => a.AnalyzeAsync(
            It.IsAny<IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)>>(),
            It.Is<IReadOnlyList<string>>(features => !features.Any()),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
