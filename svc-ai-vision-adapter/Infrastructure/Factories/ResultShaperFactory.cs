using System.Collections.Concurrent;
using svc_ai_vision_adapter.Application.Services.Shaping;

namespace svc_ai_vision_adapter.Infrastructure.Factories
{
    internal sealed class ResultShaperFactory : IResultShaperFactory
    {
        private readonly IReadOnlyDictionary<string, IResultShaper> _map;

        public ResultShaperFactory(IEnumerable<(string Key, IResultShaper Shaper)> registrations)
        {
            var dict = new Dictionary<string, IResultShaper>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, shaper) in registrations)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (!dict.TryAdd(key.Trim(), shaper))
                    throw new InvalidOperationException($"Duplicate shaper key '{key}'.");
            }

            _map = dict;
        }

        public IResultShaper Resolve(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
                throw new ArgumentException("Provider name must be set.", nameof(providerName));

            if (_map.TryGetValue(providerName, out var shaper))
                return shaper;

            throw new NotSupportedException(
                $"No result shaper registered for provider '{providerName}'. " +
                $"Registered: {string.Join(", ", _map.Keys)}");
        }
    }
}
