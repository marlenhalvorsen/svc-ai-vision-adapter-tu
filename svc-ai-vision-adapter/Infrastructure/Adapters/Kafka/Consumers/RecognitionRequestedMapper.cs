using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;

///using a mapper to keep applicationlayer kafka agnostic
namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Consumers
{
    internal static class RecognitionRequestedMapper
    {
        public static MessageKey ToDto(ImageUploadedEvent evt, string? correlationId)
            => new(new List<string> { evt.ObjectKey }, correlationId);
    }
}
