using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Infrastructure.Options;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace svc_vision_adapter_tests.Application.RecognitionServiceTest;

[TestClass]
public class ShapingAndAggregating
{
    [TestMethod]
    public async Task Calls_Shaper_And_Aggregator_For_Analyzer_Results()
    {
        // ARRANGE
        var urlFetcher = new Mock<IImageUrlFetcher>();
        var fetcher = new Mock<IImageFetcher>();
        var analyzer = new Mock<IImageAnalyzer>();
        var shaper = new Mock<IResultShaper>();
        var aggregator = new Mock<IResultAggregator>();
        var publisher = new Mock<IRecognitionCompletedPublisher>();

        var options = Options.Create(new RecognitionOptions
        {
            Features = new List<string>() // you can keep empty
        });

        // Pretend URL fetcher gives presigned URLs
        urlFetcher.Setup(f => f.FetchUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync("https://presigned.example/img.jpg");

        // Pretend we get bytes for each image
        fetcher.Setup(f => f.FetchAsync(It.IsAny<ImageRefDto>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((new ImageRefDto("img"), new byte[] { 1 }));

        // Two raw results returned by analyzer
        var raw1 = new ProviderResultDto(new ImageRefDto("img-001"), JsonDocument.Parse("{}").RootElement);
        var raw2 = new ProviderResultDto(new ImageRefDto("img-002"), JsonDocument.Parse("{}").RootElement);

        analyzer.Setup(a => a.AnalyzeAsync(
                It.IsAny<IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecognitionAnalysisResult
            {
                Provider = new AIProviderDto("Google", null, Array.Empty<string>(), null),
                InvocationMetrics = new InvocationMetricsDto(1, 1, "req"),
                Results = new List<ProviderResultDto> { raw1, raw2 }
            });

        // Define shaped results
        var shaped1 = Shape("shaped-1");
        var shaped2 = Shape("shaped-2");

        shaper.Setup(s => s.Shape(raw1)).Returns(shaped1);
        shaper.Setup(s => s.Shape(raw2)).Returns(shaped2);

        // Aggregator returns final aggregate
        var aggregate = new MachineAggregateDto { Brand = "final" };
        aggregator.Setup(a => a.Aggregate(It.IsAny<IReadOnlyList<ShapedResultDto>>()))
                  .Returns(aggregate);

        // Service under test (matches new DI setup)
        var service = new RecognitionService(
            urlFetcher.Object,
            fetcher.Object,
            options,
            analyzer.Object,
            shaper.Object,
            aggregator.Object);

        // MessageKey now represents event with multiple images
        var key = new MessageKey(
            new List<string> { "img-001", "img-002" },
            CorrelationId: "corr-001");

        // ACT
        var response = await service.AnalyzeAsync(key, CancellationToken.None);

        // ASSERT
        shaper.Verify(s => s.Shape(It.IsAny<ProviderResultDto>()), Times.Exactly(2));
        aggregator.Verify(a => a.Aggregate(It.IsAny<IReadOnlyList<ShapedResultDto>>()), Times.Once);
    }

    private static ShapedResultDto Shape(string id)
    {
        return new ShapedResultDto(
            new ImageRefDto(id),
            Machine: null,
            Evidence: null,
            Objects: Array.Empty<ObjectHitDto>()
        );
    }
}
