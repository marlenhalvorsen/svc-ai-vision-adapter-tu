using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Infrastructure.Options;
using System.Text.Json;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision.Parsing
{
    internal sealed class GoogleVisionParser
    {
        //extract the first response object from the Google Vision result
        internal JsonElement ExtractResponse(ProviderResultDto providerResult)
        {
            var raw = providerResult.Raw;

            JsonElement resp = raw;
            if (raw.ValueKind == JsonValueKind.Object
                && raw.TryGetProperty("responses", out var responses)
                && responses.ValueKind == JsonValueKind.Array
                && responses.GetArrayLength() > 0)
            {
                return resp = responses[0];
            }

            return raw;
        }


        //parseWeb
        public (IReadOnlyList<WebEntityHitDto> entities, double topScore, string bestGuess) GetWebEntities(
            JsonElement resp, 
            int recognitionOptions)
        {
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
                        .Take(recognitionOptions)
                        .ToList();
                    if (webEntities.Count > 0)
                        topWebScore = Math.Clamp(webEntities.Max(x => x.Score), 0, 1);
                }
            }
            return (webEntities, topWebScore, bestGuess);
        }
        //parselogo
        public (IReadOnlyList<LogoHitDto> logoCandidates, string logo, double logoScore) GetLogoHits(
            JsonElement resp,
            int recognitionOptions)
        {
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
                        var name = l.TryGetProperty("description", out var d) 
                        && d.ValueKind == JsonValueKind.String 
                        ? d.GetString() 
                        : null;
                        double score = (l.TryGetProperty("score", out var s) 
                        && s.ValueKind == JsonValueKind.Number 
                        && s.TryGetDouble(out var ds)) 
                        ? ds 
                        : 0;
                        return (name, score);
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.name))
                    .OrderByDescending(x => x.score)
                    .Take(recognitionOptions)
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
                    .Take(recognitionOptions)
                    .ToList();

            }
            return (logoCandidates, logo, logoscore);
        }
        //parseocr
        public string GetOcrHits(JsonElement resp)
        {
            //first try textAnnotation
            if(resp.TryGetProperty("textAnnotations", out var ta) 
                && ta.ValueKind == JsonValueKind.Array)
            {
                var first = ta.EnumerateArray().FirstOrDefault();
                if(first.ValueKind == JsonValueKind.Object
                    && first.TryGetProperty("description", out var desc)
                    && desc.ValueKind == JsonValueKind.String)
                {
                    return desc.GetString();
                }
            }

            //if there is a lot of text it will be in the fullTextAnnotation
            if(resp.TryGetProperty("fullTextAnnotation", out var fta) 
                && fta.ValueKind == JsonValueKind.Object)
            {
                var first = fta.EnumerateArray().FirstOrDefault();
                if(first.ValueKind == JsonValueKind.Object
                    && first.TryGetProperty("text", out var text)
                    && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString();
                }
            }
            return null;
        }
    }
}
