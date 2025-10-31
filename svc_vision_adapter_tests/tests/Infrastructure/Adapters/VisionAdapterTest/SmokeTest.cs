using System.IO;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Services.Aggregation;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision;
using svc_ai_vision_adapter.Infrastructure.Options;
using Assert = Xunit.Assert;
using svc_ai_vision_adapter.Application.Ports.Out;

namespace tests.Infrastructure.Adapters.VisionAdapterTest
{
    public class GoogleResultShaperSmokeTests
    {
        private readonly ITestOutputHelper _out;
        public GoogleResultShaperSmokeTests(ITestOutputHelper output) => _out = output;

        [Fact]
        public void Dump_from_fixture()
        {
            // ARRANGE
            var path = Path.Combine(
                AppContext.BaseDirectory,
                "tests", "VisionAdapterTest", "TestData", "GoogleVisionSample3.json");
            var fakeBrands = new MockBrandCatalog(new[] { "Hitachi", "Volvo", "CAT" });


            //ASSERT
            Assert.True(File.Exists(path), $"Mangler testdata: {path}");

            //ARRANGE
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var options = Options.Create(new RecognitionOptions
            {
                MaxResults = 5,
            });


            JsonElement raw = root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0
                ? results[0].GetProperty("raw").Clone()
                : root.Clone();

            //ACT
            var prov = new ProviderResultDto(new ImageRefDto("https://example/img.png"), raw);

            var shaped = new GoogleResultShaper(options, fakeBrands).Shape(prov);
            var aggregate = new ResultAggregatorService(0.70).Aggregate(new List<ShapedResultDto> { shaped });

            var pretty = JsonSerializer.Serialize(new { shaped, aggregate }, new JsonSerializerOptions { WriteIndented = true });
            _out.WriteLine(pretty); //Makes sure results are evenly spaced for readability
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
