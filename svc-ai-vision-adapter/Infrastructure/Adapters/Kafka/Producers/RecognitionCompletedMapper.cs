using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;

///mapper to convert internal dto to external event 
namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Producers
{
    internal static class RecognitionCompletedMapper
    {
        public static RecognitionCompletedEvent ToEvent(RecognitionResponseDto response)
        {
            //ensures downstream always gets a machineAggregateDto
            var aggregate = response.Aggregate ?? new MachineAggregateDto();

            return new RecognitionCompletedEvent(
                response.Ai, 
                aggregate
                );
        }
    }
}
