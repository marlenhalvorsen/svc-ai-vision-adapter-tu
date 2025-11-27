using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Contracts.Transport;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Application.Services.Aggregation;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision.Parsing;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision.Resolvers;
using svc_ai_vision_adapter.Infrastructure.Options;
using System.Text.Json;

namespace tests.Infrastructure.Adapters.VisionAdapterTest
{
    [TestClass]
    public class GoogleResultShaperSmokeTests
    {
        public TestContext? TestContext { get; set; }

        [TestMethod]
        public void Dump_from_fixture()
        {
            // ARRANGE
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "tests", "Infrastructure", "Adapters", "VisionAdapterTest", "TestData", "GoogleVisionSample3.json");

            var fakeBrands = new MockBrandCatalog(new[] { "Hitachi", "Volvo", "CAT" });
            var parser = new GoogleVisionParser();
            var brandResolver = new BrandResolver();
            var typeResolver = new TypeResolver(fakeBrands);

            Assert.IsTrue(File.Exists(path), $"Mangler testdata: {path}");

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;

            var options = Options.Create(new RecognitionOptions
            {
                MaxResults = 5,
            });

            JsonElement raw =
                root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0
                    ? results[0].GetProperty("raw").Clone()
                    : root.Clone();

            // ACT
            var prov = new ProviderResultDto(new ImageRefDto("https://example/img.png"), raw);
            var shaped = new GoogleResultShaper(options, fakeBrands, parser, brandResolver, typeResolver).Shape(prov);
            var aggregate = new ResultAggregatorService(0.70).Aggregate(new List<ShapedResultDto> { shaped });

            var pretty = JsonSerializer.Serialize(new { shaped, aggregate }, new JsonSerializerOptions { WriteIndented = true });

            // OUTPUT
            TestContext?.WriteLine(pretty);
        }

        private class MockBrandCatalog : IBrandCatalog
        {
            private readonly HashSet<string> _brands;
            public MockBrandCatalog(IEnumerable<string> brands)
            {
                _brands = new(brands, StringComparer.OrdinalIgnoreCase);
            }

            public bool IsKnownBrand(string name) => _brands.Contains(name);
            public IReadOnlyCollection<string> All => _brands;
        }
    }
}
