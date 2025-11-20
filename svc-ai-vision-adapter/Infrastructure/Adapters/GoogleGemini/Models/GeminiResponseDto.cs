using System.Text.Json.Serialization;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Models
{
    internal sealed class GeminiResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = default!;
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
        [JsonPropertyName("commentary")]
        public string? Commentary { get; set; }
        [JsonPropertyName("partial")]
        public bool? Partial { get; set; }
        [JsonPropertyName("brand")]
        public string? Brand { get; set; }
        [JsonPropertyName("machineType")]
        public string? MachineType { get; set; }
        [JsonPropertyName("model")]
        public string? Model { get; set; }
        [JsonPropertyName("weight")]
        public double? Weight { get; set; }
        [JsonPropertyName("year")]
        public string? Year { get; set; }
        [JsonPropertyName("attachment")]
        public List<string>? Attachment { get; set; }
        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }
        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }
}
