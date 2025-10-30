using svc_ai_vision_adapter.Application.Contracts;

///outgoing Kafka event when a recognitionJob is completed.
///External contract as other services will consume this.
namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models
{
    internal sealed class RecognitionCompletedEvent
    {
        public string SessionId { get; init; } = default!;
        public AIProviderDto Provider { get; init; } = default!;
        public object Aggregate { get; init; } = default!;

        public RecognitionCompletedEvent(string sessionId, AIProviderDto provider, object aggregate)
        {
            SessionId = sessionId;
            Provider = provider;
            Aggregate = aggregate;
        }

    }
}
