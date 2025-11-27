using svc_ai_vision_adapter.Application.Contracts.Transport;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;

///using a mapper to keep applicationlayer kafka agnostic
namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Consumers
{
    internal static class RecognitionRequestedMapper
    {
        public static MessageKey ToDto(ImageUploadedEvent evt, string? correlationId)
            => new(evt.ObjectKey, correlationId);
    }
}
