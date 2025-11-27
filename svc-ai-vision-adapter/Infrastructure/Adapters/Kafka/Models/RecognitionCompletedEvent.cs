using svc_ai_vision_adapter.Application.Contracts;

///outgoing Kafka event when a recognitionJob is completed.
///External contract as other services will consume this.
namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models
{
    internal sealed class RecognitionCompletedEvent
    {
        public AIProviderDto Provider { get; init; } = default!;
        //important to state the precice type of Aggregate for 
        //deserialization, or else it would deserialize as a JsonElement
        //where all fields of MachineAggregateDto would be lost
        public MachineAggregateDto Aggregate { get; init; } = default!;

        public RecognitionCompletedEvent(
            AIProviderDto provider, 
            MachineAggregateDto aggregate)
        {
            Provider = provider;
            Aggregate = aggregate;
        }

    }
}
