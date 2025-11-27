using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Outbound;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision.Resolvers
{
    public class TypeResolver
    {
        private readonly IBrandCatalog _brands;
        public TypeResolver(IBrandCatalog brandCatalog)
        {
            _brands = brandCatalog;
        }
        public string? ResolveType(string? bestGuess, IReadOnlyList<WebEntityHitDto>? webEntities, IBrandCatalog brandCatalog)
        {
            if (!String.IsNullOrWhiteSpace(bestGuess))
            {
                var cleaned = bestGuess.Trim();

                //checks if cleaned string is a brand and therefore not a type
                bool isBrand = _brands.All.Any(b =>
                cleaned.Contains(b, StringComparison.OrdinalIgnoreCase));

                if (!isBrand)
                    return cleaned;
            }

            if (webEntities != null)
            {
                foreach (var e in webEntities)
                {
                    var desc = e.Description?.Trim();
                    if (string.IsNullOrWhiteSpace(desc))
                        continue;

                    bool isBrand = _brands.All.Any(b =>
                    desc.Contains(b, StringComparison.OrdinalIgnoreCase));

                    if (!isBrand)
                        return desc;
                }
            }
            return null;
        }
    }
}
