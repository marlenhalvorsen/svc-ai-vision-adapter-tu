using Confluent.Kafka;

namespace svc_ai_vision_adapter.Infrastructure.Options
{
    internal sealed class KafkaProducerOptions
    {
        public string BootstrapServers { get; set; } = default!;
        public string Topic { get; set; } = default!;
        //leader broker confirms message recieved
        public Acks? Acks { get; set; } = Confluent.Kafka.Acks.Leader;
        public int MessageSendMaxRetries { get; set; } = 3;
    }
}
