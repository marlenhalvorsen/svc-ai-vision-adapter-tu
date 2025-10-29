using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization
{
    internal sealed class JsonKafkaSerializer : IKafkaSerializer
    {
        //ensuring both directions use the same casing rules.
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        public T Deserialize<T>(byte[] data)
        {
            //converst raw bytes to T using Json
            return JsonSerializer.Deserialize<T>(data, _options);
        }

        public byte[] Serialize<T>(T message)
        {
            //convert T to Json string
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _options));
        }
    }
}
