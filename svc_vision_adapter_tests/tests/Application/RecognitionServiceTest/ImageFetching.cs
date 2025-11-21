using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Models;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace svc_vision_adapter_tests.Application.RecognitionServiceTest;

[TestClass]
public class ImageFetching
{
    [TestMethod]
    public async Task WhenImageFetcher_IsCalled_ReturnsImages()
    {
        //ARRANGE
        var urlFetcher = new Mock<IImageUrlFetcher>();
        var fetcher = new Mock<IImageFetcher>();
        var analyzer = new Mock<IImageAnalyzer>();
        var shaper = Mock.Of<IResultShaper>();
        var aggregator = Mock.Of<IResultAggregator>();
        var publisher = Mock.Of<IRecognitionCompletedPublisher>();
        var machineReasoner = Mock.Of<IMachineReasoningAnalyzer>();
        var providerInfo = Mock.Of<IReasoningProviderInfo>();

        //Simulate URL fetcher returning presigned URLs
        urlFetcher.Setup(f => f.FetchUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://example.com/image.jpg");

        //Simulate image fetcher returning fake bytes for each image

        fetcher.Setup(f => f.FetchAsync(It.IsAny<ImageRefDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new ImageRefDto("https://example.com/image.jpg"), new byte[] { 1, 2, 3 }));

        //Minimal fake analyzer result

        analyzer.Setup(a => a.AnalyzeAsync(
                It.IsAny<IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecognitionAnalysisResult
            {
                Provider = new AIProviderDto("Google Vision", null, new List<string>(), null),
                InvocationMetrics = new InvocationMetricsDto(0, 1, "123"),
                Results = new List<ProviderResultDto>
                {
                    new ProviderResultDto(new ImageRefDto("https://example.com/image.jpg"),
                        JsonDocument.Parse("{\"result\":\"ok\"}").RootElement)
                }
            });

        var options = Options.Create(new RecognitionOptions { Features = new List<string>() });

        //RecognitionService signature
        var service = new RecognitionService(
            urlFetcher.Object,
            fetcher.Object,
            options,
            analyzer.Object,
            shaper,
            aggregator,
            machineReasoner,
            providerInfo);

        //Two images represented as object keys
        var messageKey = new MessageKey(
            new List<string> { "img-001", "img-002" },
            CorrelationId: "corr-001");

        // ACT
        await service.AnalyzeAsync(messageKey, CancellationToken.None);

        //ASSERT
        fetcher.Verify(f => f.FetchAsync(
            It.IsAny<ImageRefDto>(),
            It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // expect 2 calls (for 2 images)
    }
}
