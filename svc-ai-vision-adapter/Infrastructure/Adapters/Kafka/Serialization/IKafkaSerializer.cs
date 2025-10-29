///abstraction to own the deserializatoin of the messages

namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization
{
    internal interface IKafkaSerializer
    {
        T Deserialize<T>(byte[] data);
        byte[] Serialize<T>(T message);
    }
}
