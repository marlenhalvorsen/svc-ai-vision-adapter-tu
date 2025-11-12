using System.Text.Json;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Infrastructure.Options;
using svc_ai_vision_adapter.Application.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using svc_ai_vision_adapter.Application.Services.Shaping;
using svc_ai_vision_adapter.Application.Ports.Outbound;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision
{
    internal sealed class GoogleResultShaper : IResultShaper
    {
        private readonly IBrandCatalog _brands;
        private readonly int _maxResults; //set to 5 to limit data
        public GoogleResultShaper(IOptions<RecognitionOptions> options, IBrandCatalog brands)
        {
            _maxResults = options.Value.MaxResults;
            _brands = brands;
        }
        public ShapedResultDto Shape(ProviderResultDto r)
        {
            var raw = r.Raw;

            JsonElement resp = raw;
            if (raw.ValueKind == JsonValueKind.Object
                && raw.TryGetProperty("responses", out var responses)
                && responses.ValueKind == JsonValueKind.Array
                && responses.GetArrayLength() > 0)
            {
                resp = responses[0];
            }

            //WebDetection: bestGuessLabels + webEntities
            //Provides a series of related Web content to an image.
            string? bestGuess = null;
            List<WebEntityHitDto>? webEntities = null;
            double topWebScore = 0;
            if (resp.TryGetProperty("webDetection", out var wd) && wd.ValueKind == JsonValueKind.Object)
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
                        topWebScore = Math.Clamp(webEntities.Max(x => x.Score), 0, 1);
                }
            }

            // Logo, provides a textual description of the entity identified,
            // a confidence score, and a bounding polygon for the logo in the file.
            string? logo = null;
            double logoscore = 0;
            IReadOnlyList<LogoHitDto> logoCandidates = Array.Empty<LogoHitDto>();

            if (resp.TryGetProperty("logoAnnotations", out var logos) && logos.ValueKind == JsonValueKind.Array)
            {
                var logoHits = logos.EnumerateArray()
                    .Select(l =>
                    {
                        var name = l.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
                        double score = (l.TryGetProperty("score", out var s) && s.ValueKind == JsonValueKind.Number && s.TryGetDouble(out var ds)) ? ds : 0;
                        return (name, score);
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.name))
                    .OrderByDescending(x => x.score)
                    .Take(_maxResults)
                    .ToList();
                if (logoHits.Count > 0)
                {
                    var best = logoHits[0];
                    logo = best.name;
                    logoscore = best.score;
                    logoscore = Math.Clamp(logoscore, 0, 1);
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
            if (resp.TryGetProperty("textAnnotations", out var ta) && ta.ValueKind == JsonValueKind.Array)
            {
                var first = ta.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Object &&
                    first.TryGetProperty("description", out var desc) &&
                    desc.ValueKind == JsonValueKind.String)
                    ocr = desc.GetString();
            }
            if (string.IsNullOrWhiteSpace(ocr) &&
                resp.TryGetProperty("fullTextAnnotation", out var fta) &&
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

            if (resp.TryGetProperty("localizedObjectAnnotations", out var objs) &&
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
                                        s.TryGetDouble(out var ds)) ? ds: 0;

                        return (name, score);
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.name))
                    .GroupBy(x => x.name!, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new ObjectHitDto(g.Key, g.Max(x => x.score)))
                    .OrderByDescending(x => x.Score)
                    .Take(_maxResults)
                    .ToList();
            }

            string? resolvedType = null;
            double typeScore = 0.0;

            // BRAND resolution (data-driven: logo -> brand from OCR using catalog)
            string? resolvedBrand = null;
            double brandScore = 0.0;

            if (!string.IsNullOrWhiteSpace(logo))
            {
                resolvedBrand = logo;
                brandScore = Math.Max(logoscore, 0.90);
            }
            else
            {
                // Builds a stoplist of known type-related terms 
                // to avoid picking a machine category as a brand.
                var typeStop = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(resolvedType))
                    typeStop.Add(resolvedType);
                if (webEntities is not null)
                    foreach (var we in webEntities)
                    {
                        var desc = we.Description?.Trim();
                        if (string.IsNullOrWhiteSpace(desc))
                            continue; 

                        bool containsBrand = _brands.All.Any(b =>
                        desc.Contains(b, StringComparison.OrdinalIgnoreCase));


                        if (!containsBrand)
                            typeStop.Add(desc);
                    }                       
                if (objects is not null)
                    foreach (var o in objects)
                    {
                        var name = o.Name?.Trim();
                        if (string.IsNullOrWhiteSpace(name))
                            continue; 
                        
                        bool containsBrand = _brands.All.Any(b =>
                        name.Contains(b, StringComparison.OrdinalIgnoreCase));

                        if(!containsBrand)
                            typeStop.Add(name);
                    }
                if (!string.IsNullOrWhiteSpace(bestGuess))
                {
                    var best = bestGuess?.Trim();

                    bool containsBrand = _brands.All.Any(b =>
                    best.Contains(b, StringComparison.OrdinalIgnoreCase));

                    if(!containsBrand)
                        typeStop.Add(best);
                }

                // Try to find a known brand name from the OCR text using the catalog.
                string? bestMatch = null;
                int bestLength = 0;

                foreach (var brandName in _brands.All)
                {
                    //skip brand that is also in the stoplist (ex "bulldozer")
                    if (typeStop.Contains(brandName))
                        continue;

                    //look for whole word matches
                    //allows multi-word brands like "John Deere"
                    var ocrText = ocr ?? string.Empty;
                    var pattern = $@"\b{Regex.Escape(brandName)}\b";
                    if (Regex.IsMatch(ocrText ?? string.Empty, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    {
                        if (brandName.Length > bestLength)
                        {
                            bestLength = brandName.Length;
                            bestMatch = brandName;
                        }
                    }
                }

                // If any brand was found in OCR, assign it; otherwise leave brand null.
                if (!string.IsNullOrWhiteSpace(bestMatch))
                {
                    resolvedBrand = bestMatch;
                    brandScore = 0.80;
                }
            }


            // Confidence
            double topObjectScore = objects.Count > 0 ? objects[0].Score : 0;
            brandScore = Math.Clamp(brandScore, 0, 1);
            //all scores are already set with Math.Clamp to be in between 0-1
            double confidence = new[] { topObjectScore, logoscore, topWebScore, brandScore }.Max(); 
            var summary = new MachineSummaryDto(
                Type: resolvedType,
                Brand: resolvedBrand,
                Model: null,
                Confidence: confidence,
                IsConfident: confidence >= 0.5
            );

            var evidence = new EvidenceDto(
                WebBestGuess: bestGuess,
                Logo: logo,
                OcrSample: ocr,
                WebEntities: webEntities,
                Objects: objects,
                LogoCandidates: logoCandidates
            );

            return new ShapedResultDto(r.ImageRef, summary, evidence, objects);
        }

    }
}
