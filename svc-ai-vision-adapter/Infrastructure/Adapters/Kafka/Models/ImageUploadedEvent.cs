using svc_ai_vision_adapter.Application.Contracts;
using System.Collections.Generic;
using System.Text.Json.Serialization;

///Representation of the shape of the message that is published
///on the Kafka topic. Therefore the "wire contract" /integration contract
namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models
{
    internal sealed class ImageUploadedEvent
    {
        [JsonPropertyName("objectKey")]
        public string ObjectKey { get; init; } = default!;
        [JsonPropertyName("correlationId")]

        public string? CorrelationId { get; init; } 
    }
}
