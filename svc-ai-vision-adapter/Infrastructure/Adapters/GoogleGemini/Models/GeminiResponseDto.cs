namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Models
{
    internal sealed class GeminiResponseDto
    {
        public string Status { get; set; } = default!;
        public string? Reason { get; set; }
        public string? Commentary { get; set; }
        public bool? Partial { get; set; }
        public string? Brand { get; set; }
        public string? MachineType { get; set; }
        public string? Model { get; set; }
        public double? Weight { get; set; }
        public string? Year { get; set; }
        public List<string>? Attachment { get; set; }
        public double? Confidence { get; set; }
        public string? Source { get; set; }
    }
}
