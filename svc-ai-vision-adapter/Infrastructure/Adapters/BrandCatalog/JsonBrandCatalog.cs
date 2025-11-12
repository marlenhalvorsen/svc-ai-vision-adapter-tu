using svc_ai_vision_adapter.Application.Ports.Outbound;
using System.Text.Json;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.BrandCatalog
{
    public class JsonBrandCatalog : IBrandCatalog
    {
        private readonly HashSet<string> _brands;
        public JsonBrandCatalog(string JsonPath)
        {
            var json = File.ReadAllText(JsonPath);
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new();
            _brands = new(list, StringComparer.OrdinalIgnoreCase);
        }
        public bool IsKnownBrand(string name) =>
         !string.IsNullOrWhiteSpace(name) && _brands.Contains(name);
        public IReadOnlyCollection<string> All => _brands;
    }
}
