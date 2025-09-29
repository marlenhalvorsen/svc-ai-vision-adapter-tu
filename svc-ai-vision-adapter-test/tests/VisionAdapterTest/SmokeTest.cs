using System.IO;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Services;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision;
using svc_ai_vision_adapter.Infrastructure.Options;
using Assert = Xunit.Assert;

namespace SvcAiVisionAdapter.Tests.VisionAdapterTest
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
                System.AppContext.BaseDirectory,
                "tests", "VisionAdapterTest", "TestData", "GoogleVisionSample.json");

            //ASSERT
            Assert.True(File.Exists(path), $"Mangler testdata: {path}");

            //ARRANGE
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var options = Options.Create(new RecognitionOptions
            {
                MaxResults = 5,
                // udfyld evt. andre felter hvis nødvendigt
            });


            JsonElement raw = root.TryGetProperty("results", out var results) && results.GetArrayLength() > 0
                ? results[0].GetProperty("raw").Clone()
                : root.Clone();

            //ACT
            var prov = new ProviderResultDto(new ImageRefDto("https://example/img.png"), raw);

            var shaped = new GoogleResultShaper(options).Shape(prov);
            var aggregate = new ResultAggregatorService(0.70).Aggregate(new System.Collections.Generic.List<ShapedResultDto> { shaped });

            var pretty = JsonSerializer.Serialize(new { shaped, aggregate }, new JsonSerializerOptions { WriteIndented = true });
            _out.WriteLine(pretty); //Makes sure results are evenly spaced for readability
        }
    }
}
