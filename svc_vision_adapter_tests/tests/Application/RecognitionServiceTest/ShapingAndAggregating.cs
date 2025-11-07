using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Out;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Application.Services.Factories;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Infrastructure.Options;
using System.Text.Json;
using Microsoft.Extensions.Options;


namespace svc_vision_adapter_tests.Application.RecognitionServiceTest;

[TestClass]
public class ShapingAndAggregating
{
    [TestMethod]
    public async Task TestMethod1()
    {
        var fetcher = new Mock<IImageFetcher>();
        var analyzer = new Mock<IImageAnalyzer>();
        var factory = new Mock<IAnalyzerFactory>();
        var shaper = new Mock<IResultShaper>();
        var aggregator = new Mock<IResultAggregator>();
        var publisher = new Mock<IRecognitionCompletedPublisher>();


        //factory returns analyzer 
        factory 
            .Setup(a => a.Resolve(It.IsAny<string>()))
            .Returns(analyzer.Object);

        // Fetcher returns dummy bytes
        fetcher.Setup(f => f.FetchAsync(It.IsAny<ImageRefDto>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((new ImageRefDto("img"), new byte[] { 1 }));

        // Create two raw results (input for shaper)
        var raw1 = new ProviderResultDto(new ImageRefDto("img-001"), JsonDocument.Parse("{}").RootElement);
        var raw2 = new ProviderResultDto(new ImageRefDto("img-002"), JsonDocument.Parse("{}").RootElement);
        analyzer.Setup(a => a.AnalyzeAsync(
            It.IsAny<IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)>>(),
            It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new RecognitionAnalysisResult
        {
            Provider = new AIProviderDto("Google", null, null, Array.Empty<string>(), null),
            InvocationMetrics = new InvocationMetricsDto(1, 1, "req"),
            Results = new List<ProviderResultDto> { raw1, raw2 }
        });

        // Define shaped results
        var shaped1 = Shape("shaped-1");
        var shaped2 = Shape("shaped-2");

        shaper.Setup(s => s.Shape(raw1)).Returns(shaped1);
        shaper.Setup(s => s.Shape(raw2)).Returns(shaped2);

        // Aggregator returns final aggregate result
        var aggregate = new MachineAggregateDto("final");
        aggregator.Setup(a => a.Aggregate(It.IsAny<IReadOnlyList<ShapedResultDto>>()))
                  .Returns(aggregate);

        var service = new RecognitionService(
            factory.Object, 
            fetcher.Object, 
            Options.Create(new RecognitionOptions()), 
            shaper.Object, 
            aggregator.Object, 
            publisher.Object
            );

        var request = new RecognitionRequestDto(
            "session", 
            new List<ImageRefDto> { new("img-001") }, 
            "Google"
            );

        // ACT
        var response = await service.AnalyzeAsync(request, CancellationToken.None);

        // ASSERT

    
    }
    public static ShapedResultDto Shape(string id)
    {
        return new ShapedResultDto(
            new ImageRefDto(id),
            Machine: null,
            Evidence: null,
            Objects: Array.Empty<ObjectHitDto>()
        );
    }
}
