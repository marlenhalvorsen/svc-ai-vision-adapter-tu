using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Application.Services.Aggregation;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision;
using svc_ai_vision_adapter.Infrastructure.Options;
using svc_vision_adapter_tests.Fakes;

namespace svc_vision_adapter_tests.tests.Application.RecognitionServiceTest
{
    [TestClass]
    public class RecognitionServiceTests
    {
        private RecognitionService _sut;
        private Mock<IMachineReasoningAnalyzer> _fakeReasoner;


        [TestInitialize]
        public void Setup()
        {
            var fakeUrlFetcher = new FakeImageUrlFetcher();
            var fakeFetcher = new FakeImageFetcher();
            var fakeAnalyzer = new FakeImageAnalyzer();

            var options = Options.Create(new RecognitionOptions
            {
                MaxResults = 5,
                IncludeRaw = true,
                Features = new List<string> { "LOGO_DETECTION" }
            });

            var fakeBrandCatalog = new FakeBrandCatalog(new[] { "Hitachi", "Volvo", "CAT", "Caterpillar" });
            var shaper = new GoogleResultShaper(options, fakeBrandCatalog);
            var aggregator = new ResultAggregatorService(0.70);

            // MOCK Gemini machine reasoning analyzer as this calls external API
            _fakeReasoner = new Mock<IMachineReasoningAnalyzer>();

            // what should reasoning return
            _fakeReasoner
                .Setup(r => r.AnalyzeAsync(It.IsAny<MachineAggregateDto>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MachineAggregateDto
                {
                    Brand = "Caterpillar",
                    MachineType = "Wheel Loader",
                    Model = "930G",
                    Confidence = 0.90,
                    IsConfident = true,
                    TypeSource = "mocked"
                });

            _sut = new RecognitionService(
                fakeUrlFetcher,
                fakeFetcher,
                options,
                fakeAnalyzer,
                shaper,
                aggregator,
                _fakeReasoner.Object
            );
        }

        [TestMethod]
        public async Task AnalyzeAsync_Should_Return_Valid_Response()
        {
            // Arrange
            var messageKey = new MessageKey(
                new List<string> { "images/2025/11/02/img.jpg" },
                CorrelationId: "corr-123"
            );

            // Act
            var result = await _sut.AnalyzeAsync(messageKey);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("FakeVision", result.Ai.Name);
            Assert.AreEqual("corr-123", result.CorrelationId);
            Assert.IsTrue(result.Results.Count > 0);
            Assert.IsNotNull(result.Aggregate);
            Assert.IsTrue(result.Aggregate.IsConfident);
            StringAssert.Contains(result.Aggregate.Brand, "Caterpillar Inc");
        }

        private class FakeBrandCatalog : IBrandCatalog
        {
            private readonly HashSet<string> _brands;

            public FakeBrandCatalog(IEnumerable<string> brands)
            {
                _brands = new HashSet<string>(brands, StringComparer.OrdinalIgnoreCase);
            }

            public bool IsKnownBrand(string name) => _brands.Contains(name);
            public IReadOnlyCollection<string> All => _brands;
        }
    }
}
