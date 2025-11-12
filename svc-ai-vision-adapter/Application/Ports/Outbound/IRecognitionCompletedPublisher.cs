using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;

///When a recognition is completed the event is published by 
///RecognitionCompletedKafkaProducer to an external messagebroker. 
namespace svc_ai_vision_adapter.Application.Ports.Outbound
{
    public interface IRecognitionCompletedPublisher
    {
        Task PublishAsync(RecognitionResponseDto response, CancellationToken ct);
    }
}
