using svc_ai_vision_adapter.Application.Ports.Outbound;

namespace svc_ai_vision_adapter.Infrastructure.Options
{
    public sealed class GeminiOptions : IReasoningProviderInfo
    {
        public string ApiKey { get; set; } = default!;
        public string Model { get; set; } = default!;
        public string PromptPath { get; set; } = default!;
        public string SchemaPath { get; set; } = default!;
        public string Name => "Gemini";
    }
}
