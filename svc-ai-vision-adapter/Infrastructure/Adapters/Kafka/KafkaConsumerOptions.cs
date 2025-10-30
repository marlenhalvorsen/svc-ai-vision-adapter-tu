//config object for the Kafka consumer

namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka
{
    internal sealed class KafkaConsumerOptions
    {
        //address for Kafka-broker
        public string BootstrapServers { get; set; } = default!; 
        //what instance that gets the message
        public string GroupId { get; set; } = default!;
        public string Topic { get; set; } = default!;
        //autocommit = true -> Kafka marks messages as read automatically
        //false -> user have to call _consumer.Commit() after read
        public bool EnableAutoCommit { get; set; } = true;
    }
}
