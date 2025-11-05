using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Out;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Application.Services.Factories;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Grpc.Core;


namespace svc_vision_adapter_tests;

[TestClass]
public class ImageFetching
{
    [TestMethod]
    public async Task WhenImageFetcher_IsCalled_ReturnsImages()
    {
        //ARRANGE
        var factory = new Mock<IAnalyzerFactory>();
        var fetcher = new Mock<IImageFetcher>();
        var analyzer = new Mock<IImageAnalyzer>();
        var shaper = Mock.Of<IResultShaper>();
        var aggregator = Mock.Of<IResultAggregator>();
        var publisher = Mock.Of<IRecognitionCompletedPublisher>();

        // Factory returns our mock analyzer
        factory
            .Setup(f => f.Resolve(It.IsAny<string>()))
            .Returns(analyzer.Object);

        // Return fake bytes when fetching image
        fetcher.Setup(f => f.FetchAsync(
            It.IsAny<ImageRefDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new ImageRefDto("img-001"), new byte[] { 1, 2, 3 }));

        // Analyzer returns minimal fake result
        analyzer.Setup(a => a.AnalyzeAsync(
                It.IsAny<IReadOnlyList<(ImageRefDto Ref, byte[] Bytes)>>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecognitionAnalysisResult());


        var options = Options.Create(new RecognitionOptions
        {
            Features = new List<string>()
        });

        var service = new RecognitionService(factory.Object, fetcher.Object, options, shaper, aggregator, publisher);

        var images = new List<ImageRefDto>
        {
            new ("img-001"),
            new("img-002")
        };

        var request = new RecognitionRequestDto("session-1", images, "Google Vision");

        //ACT
        await service.AnalyzeAsync(request);

        //ASSERT
        fetcher.Verify(a => a.FetchAsync(
            It.IsAny<ImageRefDto>(),
            It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
