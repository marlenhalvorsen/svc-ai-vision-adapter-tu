using System.Text.Json;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision
{
    internal sealed class GoogleResultShaper : IResultShaper
    {
        public ShapedResultDto Shape(ProviderResultDto r)
        {
            var raw = r.Raw;

            // Labels (navn + score)
            var labelHits = new List<(string Name, double Score)>();
            if (raw.TryGetProperty("labelAnnotations", out var la) && la.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in la.EnumerateArray())
                {
                    if (item.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String)
                    {
                        var name = d.GetString()!;
                        double score = 0;
                        if (item.TryGetProperty("score", out var s) && s.ValueKind == JsonValueKind.Number && s.TryGetDouble(out var ds))
                            score = ds;
                        labelHits.Add((name, score));
                    }
                }
            }

            // Logo (første beskrivelse)
            string? logo = null;
            if (raw.TryGetProperty("logoAnnotations", out var logos) && logos.ValueKind == JsonValueKind.Array)
            {
                foreach (var l in logos.EnumerateArray())
                {
                    if (l.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String)
                    {
                        var val = d.GetString();
                        if (!string.IsNullOrWhiteSpace(val)) { logo = val; break; }
                    }
                }
            }

            // OCR (kort sample)
            string? ocr = null;
            if (raw.TryGetProperty("fullTextAnnotation", out var fta) && fta.ValueKind == JsonValueKind.Object &&
                fta.TryGetProperty("text", out var txt) && txt.ValueKind == JsonValueKind.String)
            {
                var t = txt.GetString();
                if (!string.IsNullOrWhiteSpace(t))
                    ocr = t!.Length > 200 ? t[..200] + "…" : t;
            }

            // Web best guess
            string? bestGuess = null;
            if (raw.TryGetProperty("webDetection", out var web) && web.ValueKind == JsonValueKind.Object &&
                web.TryGetProperty("bestGuessLabels", out var bgl) && bgl.ValueKind == JsonValueKind.Array)
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

            // Objects (navn + score)
            var objects = new List<ObjectHitDto>();
            if (raw.TryGetProperty("localizedObjectAnnotations", out var objs) && objs.ValueKind == JsonValueKind.Array)
            {
                foreach (var obj in objs.EnumerateArray())
                {
                    var name = obj.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString()! : "object";
                    double score = 0;
                    if (obj.TryGetProperty("score", out var sc) && sc.ValueKind == JsonValueKind.Number && sc.TryGetDouble(out var d))
                        score = d;
                    objects.Add(new ObjectHitDto(name, score));
                }
            }

            // Top label + vægtet confidence (brug label-score hvis tilgængelig, ellers top object)
            var topLabel = labelHits.OrderByDescending(x => x.Score).FirstOrDefault();
            var topObj = objects.OrderByDescending(o => o.Score).FirstOrDefault();
            var confidence = topLabel.Score > 0 ? topLabel.Score : (topObj?.Score ?? 0);

            var summary = new MachineSummaryDto(
                Type: topLabel.Name ?? labelHits.FirstOrDefault().Name, // fallback
                Brand: logo,
                Model: null,
                Confidence: confidence,
                IsConfident: confidence >= 0.7
            );

            var evidence = new EvidenceDto(
                WebBestGuess: bestGuess,
                TopLabels: labelHits.OrderByDescending(x => x.Score).Select(x => x.Name).Take(5).ToList(),
                Logo: logo,
                OcrSample: ocr
            );

            return new ShapedResultDto(r.ImageRef, summary, evidence, objects);
        }
    }
}
