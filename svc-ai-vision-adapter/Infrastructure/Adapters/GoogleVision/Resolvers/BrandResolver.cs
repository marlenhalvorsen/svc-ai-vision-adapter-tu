using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using System.Text.RegularExpressions;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision.Resolvers
{
    public class BrandResolver
    {
        public (string? resolvedBrand, double brandScore) ResolveBrand(
            string? logo,
            double logoScore,
            IReadOnlyList<WebEntityHitDto>? webEntities,
            IBrandCatalog brands,
            string? bestGuess, 
            string? ocrText)
        {
            string resolvedBrand = string.Empty;
            double brandScore = 0;
            if (!string.IsNullOrWhiteSpace(logo))
            {
                resolvedBrand = logo;
                brandScore = Math.Max(logoScore, 0.90);
            }
            else
            {
                // Builds a stoplist of known type-related terms 
                // to avoid picking a machine category as a brand.
                var typeStop = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (webEntities is not null)
                    foreach (var we in webEntities)
                    {
                        var desc = we.Description?.Trim();
                        if (string.IsNullOrWhiteSpace(desc))
                            continue;

                        bool containsBrand = brands.All.Any(b =>
                        desc.Contains(b, StringComparison.OrdinalIgnoreCase));


                        if (!string.IsNullOrWhiteSpace(desc) && !containsBrand)
                            typeStop.Add(desc);
                    }
                if (!string.IsNullOrWhiteSpace(bestGuess))
                {
                    var best = bestGuess.Trim();

                    bool containsBrand = brands.All.Any(b =>
                    best.Contains(b, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(best) && !containsBrand)
                        typeStop.Add(best);
                }

                // Try to find a known brand name from the OCR text using the catalog.
                string? bestMatch = null;
                int bestLength = 0;

                foreach (var brandName in brands.All)
                {
                    //skip brand that is also in the stoplist (ex "bulldozer")
                    if (typeStop.Contains(brandName))
                        continue;

                    //look for whole word matches
                    //allows multi-word brands like "John Deere"
                    var text = ocrText ?? string.Empty;
                    var pattern = $@"\b{Regex.Escape(brandName)}\b";
                    if (Regex.IsMatch(text ?? string.Empty, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
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
            brandScore = Math.Clamp(brandScore, 0, 1);

            return (resolvedBrand, brandScore);
        }
    }
}
