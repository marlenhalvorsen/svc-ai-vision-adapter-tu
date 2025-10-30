using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;

///using a mapper to keep applicationlayer kafka agnostic
namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Consumers
{
    internal static class RecognitionRequestedMapper
    {
        public static RecognitionRequestDto ToDto(RecognitionRequestedEvent evt)
        {
            return new RecognitionRequestDto(
                evt.RequestID, 
                evt.ImageUrls, 
                evt.Provider
                );   
        }
    }
}
