using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Options;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Prompt
{
    internal sealed class GeminiPromptLoader
    {
        private readonly string _promptTemplate;
        public GeminiPromptLoader(IOptions<GeminiOptions> opt)
        {
            var path = opt.Value.PromptPath;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Gemini prompt file not found: {path}");
            };

            _promptTemplate = File.ReadAllText(path);
        }
        public string BuildPrompt(MachineAggregateDto aggregate)
        {
            return _promptTemplate
                .Replace("{{brand}}", aggregate.Brand ?? "unknown")
                .Replace("{{type}}", aggregate.Type ?? "unknown")
                .Replace("{{model}}", aggregate.Model ?? "unknown");
        }
    }
}
