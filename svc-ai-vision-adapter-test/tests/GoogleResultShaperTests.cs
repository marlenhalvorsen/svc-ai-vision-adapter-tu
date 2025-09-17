//using System.Text;
//using System.Text.Json;
//using FluentAssertions;
//using svc_ai_vision_adapter.Application.Contracts;
//using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision;
//using Xunit;

//public class GoogleResultShaperTests
//{
//    private static ProviderResultDto Build(string json, string url = "https://example/img.png")
//    {
//        var raw = JsonDocument.Parse(json).RootElement;
//        return new ProviderResultDto(new ImageRefDto(url), raw);
//    }

//    [Fact]
//    public void Shape_extracts_top_label_logo_web_ocr_objects_and_builds_summary()
//    {
//        // Arrange: fuldt “happy path”-payload
//        var json = """
//        {
//          "labelAnnotations": [
//            { "description": "Keyboard", "score": 0.81 },
//            { "description": "Electronics", "score": 0.66 },
//            { "description": "Peripheral", "score": 0.72 }
//          ],
//          "logoAnnotations": [
//            { "description": "" },
//            { "description": "LogiTech" }
//          ],
//          "fullTextAnnotation": {
//            "text": "This is OCR text that is short."
//          },
//          "webDetection": {
//            "bestGuessLabels": [
//              { "label": "" },
//              { "label": "computer keyboard" }
//            ]
//          },
//          "localizedObjectAnnotations": [
//            { "name": "Key", "score": 0.62 },
//            { "name": "Button", "score": 0.55 }
//          ]
//        }
//        """;

//        var shaper = new GoogleResultShaper();
//        var shaped = shaper.Shape(Build(json));

//        // Assert: Summary
//        shaped.Summary.Type.Should().Be("Keyboard"); // top label by score 0.81
//        shaped.Summary.Brand.Should().Be("LogiTech"); // første non-empty logo
//        shaped.Summary.Model.Should().BeNull();
//        shaped.Summary.Confidence.Should().BeApproximately(0.81, 1e-9); // label vinder over objects
//        shaped.Summary.IsConfident.Should().BeTrue(); // >= 0.7

//        // Assert: Evidence
//        shaped.Evidence.WebBestGuess.Should().Be("computer keyboard");
//        shaped.Evidence.Logo.Should().Be("LogiTech");
//        shaped.Evidence.OcrSample.Should().Be("This is OCR text that is short.");
//        shaped.Evidence.TopLabels.Should().ContainInOrder("Keyboard", "Peripheral", "Electronics"); // sorteret desc, top5

//        // Assert: Objects
//        shaped.Objects.Should().HaveCount(2);
//        shaped.Objects[0].Name.Should().Be("Key");
//        shaped.Objects[0].Score.Should().BeApproximately(0.62, 1e-9);
//    }

//    [Fact]
//    public void Shape_truncates_ocr_after_200_chars_and_appends_ellipsis()
//    {
//        // Arrange: OCR tekst på 201 tegn
//        var longText = new string('A', 201);
//        var json = $$"""
//        { "fullTextAnnotation": { "text": "{{longText}}" } }
//        """;

//        var shaper = new GoogleResultShaper();
//        var shaped = shaper.Shape(Build(json));

//        shaped.Evidence.OcrSample.Should().NotBeNull();
//        shaped.Evidence.OcrSample!.Length.Should().Be(201); // 200 + '…'
//        shaped.Evidence.OcrSample![..200].Should().Be(new string('A', 200));
//        shaped.Evidence.OcrSample![200].Should().Be('…');   // U+2026
//    }

//    [Fact]
//    public void Shape_keeps_ocr_as_is_when_length_is_200_or_less()
//    {
//        var exactly200 = new string('B', 200);
//        var json = $$"""{ "fullTextAnnotation": { "text": "{{exactly200}}" } }""";

//        var shaper = new GoogleResultShaper();
//        var shaped = shaper.Shape(Build(json));

//        shaped.Evidence.OcrSample.Should().Be(exactly200); // ingen ellipsis
//    }

//    [Fact]
//    public void Shape_confidence_falls_back_to_top_object_when_no_labels()
//    {
//        var json = """
//        {
//          "localizedObjectAnnotations": [
//            { "name": "Box", "score": 0.69 },
//            { "name": "Bag", "score": 0.73 }
//          ]
//        }
//        """;

//        var shaper = new GoogleResultShaper();
//        var shaped = shaper.Shape(Build(json));

//        shaped.Summary.Confidence.Should().BeApproximately(0.73, 1e-9); // top object
//        shaped.Summary.IsConfident.Should().BeTrue(); // 0.73 >= 0.7

//        // Type er baseret på labels – når der ingen labels er, accepterer vi null
//        shaped.Summary.Type.Should().BeNull();
//    }

//    [Fact]
//    public void Shape_handles_missing_fields_gracefully()
//    {
//        var json = "{}"; // alt mangler

//        var shaper = new GoogleResultShaper();
//        var shaped = shaper.Shape(Build(json));

//        shaped.Summary.Brand.Should().BeNull();
//        shaped.Summary.Type.Should().BeNull();
//        shaped.Summary.Model.Should().BeNull();
//        shaped.Summary.Confidence.Should().Be(0);
//        shaped.Summary.IsConfident.Should().BeFalse();

//        shaped.Evidence.WebBestGuess.Should().BeNull();
//        shaped.Evidence.Logo.Should().BeNull();
//        shaped.Evidence.OcrSample.Should().BeNull();
//        shaped.Evidence.TopLabels.Should().BeEmpty();

//        shaped.Objects.Should().BeEmpty();
//    }

//    [Fact]
//    public void Shape_ignores_whitespace_logo_and_picks_first_nonempty()
//    {
//        var json = """
//        {
//          "logoAnnotations": [
//            { "description": "   " },
//            { "description": "BrandX" }
//          ]
//        }
//        """;

//        var shaper = new GoogleResultShaper();
//        var shaped = shaper.Shape(Build(json));

//        shaped.Summary.Brand.Should().Be("BrandX");
//        shaped.Evidence.Logo.Should().Be("BrandX");
//    }

//    [Fact]
//    public void Shape_limits_top_labels_to_five_and_sorts_descending()
//    {
//        var labels = string.Join(",", Enumerable.Range(1, 8)
//            .Select(i => $@"{{ ""description"": ""L{i}"", ""score"": {0.10 * i:0.00} }}"));

//        var json = $$"""
//        { "labelAnnotations": [ {{labels}} ] }
//        """;

//        var shaper = new GoogleResultShaper();
//        var shaped = shaper.Shape(Build(json));

//        shaped.Evidence.TopLabels.Should().HaveCount(5);
//        shaped.Evidence.TopLabels.Should().ContainInOrder("L8", "L7", "L6", "L5", "L4");
//    }
//}
