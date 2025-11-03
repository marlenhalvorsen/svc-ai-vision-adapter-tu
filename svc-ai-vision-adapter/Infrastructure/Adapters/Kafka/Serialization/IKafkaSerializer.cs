///abstraction to own the deserialization and serialization of the messages.
///Easier to test and mock for simulation of producer/consumer
///as well as ensuring separation of concern

namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization
{
    public interface IKafkaSerializer
    {
        T Deserialize<T>(byte[] data);
        byte[] Serialize<T>(T message);
    }
}
