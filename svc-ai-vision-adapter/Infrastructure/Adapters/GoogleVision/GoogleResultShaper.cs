using System.Text.Json;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Infrastructure.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;
using System.Linq;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision
{
    internal sealed class GoogleResultShaper : IResultShaper
    {
        private readonly int _maxResults; //set to 5 to limit data
        public GoogleResultShaper(IOptions<RecognitionOptions> options)
        {
            _maxResults = options.Value.MaxResults;
        }
        public ShapedResultDto Shape(ProviderResultDto r)
        {
            var raw = r.Raw;

            //WebDetection: bestGuessLabels + webEntities
            //Provides a series of related Web content to an image.
            string? bestGuess = null;
            List<WebEntityHitDto>? webEntities = null;
            double topWebScore = 0;
            if (raw.TryGetProperty("webDetection", out var wd) && wd.ValueKind == JsonValueKind.Object)
            {
                //bestGuessLabels -> first non-empty as google sorts from best - worst
                //A best guess as to the topic of the requested image inferred from similar images on the Internet.
                if (wd.TryGetProperty("bestGuessLabels", out var bgl) && bgl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var bg in bgl.EnumerateArray())
                    {
                        if (bg.TryGetProperty("label", out var l) && l.ValueKind == JsonValueKind.String)
                        {
                            var val = l.GetString();
                            if (!string.IsNullOrWhiteSpace(val)) { bestGuess = val; break; }
                        }
                    }
                }

                //WebEntities -> Name + Score (top 5)
                //Inferred entities (labels/descriptions) from similar images on the Web.
                if (wd.TryGetProperty("webEntities", out var we) && we.ValueKind == JsonValueKind.Array)
                {
                    webEntities = we.EnumerateArray()
                        .Select(e =>
                        {
                            var name = e.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String
                                       ? d.GetString()
                                       : null;
                            double score = 0;
                            if (e.TryGetProperty("score", out var s) && s.ValueKind == JsonValueKind.Number && s.TryGetDouble(out var ds))
                                score = ds;
                            return (name, score);
                        })
                        .Where(x => !string.IsNullOrWhiteSpace(x.name))
                        .Select(x => new WebEntityHitDto(x.name!, x.score))
                        .OrderByDescending(x => x.Score)
                        .Take(_maxResults)
                        .ToList();
                    if (webEntities.Count > 0)
                        topWebScore = webEntities.Max(x => x.Score);
                }
            }

            // Logo, provides a textual description of the entity identified,
            // a confidence score, and a bounding polygon for the logo in the file.
            string? logo = null;
            double logoscore = 0;
            IReadOnlyList<LogoHitDto> logoCandidates = Array.Empty<LogoHitDto>();

            if (raw.TryGetProperty("logoAnnotations", out var logos) && logos.ValueKind == JsonValueKind.Array)
            {
                var logoHits = logos.EnumerateArray()
                    .Select(l =>
                    {
                        var name = l.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                        double score = (l.TryGetProperty("score", out var s) && s.ValueKind == JsonValueKind.Number && s.TryGetDouble(out var ds)) ? ds : 0;
                        return (name, score); 
                    })
                    .Where(x=> !string.IsNullOrWhiteSpace(x.name))
                    .OrderByDescending(x => x.score)
                    .Take(_maxResults)
                    .ToList();
                if(logoHits.Count> 0)
                {
                    var best = logoHits[0];
                    logo = best.name; 
                    logoscore = best.score;
                }

                logoCandidates = logoHits
                    .GroupBy(x => x.name!, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new LogoHitDto(g.Key, g.Max(v => v.score)))
                    .OrderByDescending(x => x.Score)
                    .Take(_maxResults)
                    .ToList();
            }

            // OCR handling:Optical character recognition (OCR) for an image;
            // text recognition and conversion to machine-coded text.
            // Identifies and extracts UTF-8 text in an image.
            // First, try TEXT_DETECTION (optimized for sparse text in images).
            // Uses textAnnotations[0].description for a quick extraction.
            // If not available, fall back to DOCUMENT_TEXT_DETECTION,
            // which provides fullTextAnnotation (better for dense text or handwriting).
            string? ocr = null;
            if (raw.TryGetProperty("textAnnotations", out var ta) && ta.ValueKind == JsonValueKind.Array)
            {
                var first = ta.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Object &&
                    first.TryGetProperty("description", out var desc) &&
                    desc.ValueKind == JsonValueKind.String)
                    ocr = desc.GetString();
            }
            if (string.IsNullOrWhiteSpace(ocr) &&
                raw.TryGetProperty("fullTextAnnotation", out var fta) &&
                fta.ValueKind == JsonValueKind.Object &&
                fta.TryGetProperty("text", out var ftaText) &&
                ftaText.ValueKind == JsonValueKind.String)
            {
                ocr = ftaText.GetString();
            }


            //Object Localization (limit to MaxResults)
            //Provides general label and bounding box annotations
            //for multiple objects recognized in a single image.
            List<ObjectHitDto> objects = new();

            if (raw.TryGetProperty("localizedObjectAnnotations", out var objs) &&
                objs.ValueKind == JsonValueKind.Array)
            {
                objects = objs.EnumerateArray()
                    .Select(o =>
                    {
                        string? name = o.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
                            ? n.GetString()
                            : null;

                        double score = (o.TryGetProperty("score", out var s) &&
                                        s.ValueKind == JsonValueKind.Number &&
                                        s.TryGetDouble(out var ds))
                            ? ds
                            : 0;

                        return (name, score);
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.name))                          
                    .GroupBy(x => x.name!, StringComparer.OrdinalIgnoreCase)                 
                    .Select(g => new ObjectHitDto(g.Key, g.Max(x => x.score)))              
                    .OrderByDescending(x => x.Score)
                    .Take(_maxResults)
                    .ToList();
            }

            double topObjectScore = objects.Count > 0 ? objects[0].Score : 0;

            double confidence = Math.Max(
                topObjectScore, 
                Math.Max(logoscore, Math.Min(1.0, topWebScore)));

            var summary = new MachineSummaryDto(
                Type: null,
                Brand: logo,
                Model: null,
                Confidence: confidence,
                IsConfident: confidence >= 0.7
            );

            var evidence = new EvidenceDto(
                WebBestGuess: bestGuess,
                Logo: logo,
                OcrSample: ocr,
                WebEntities: webEntities,
                Objects : objects,
                LogoCandidates : logoCandidates
            );

            return new ShapedResultDto(r.ImageRef, summary, evidence, objects);
        }

    }
}
