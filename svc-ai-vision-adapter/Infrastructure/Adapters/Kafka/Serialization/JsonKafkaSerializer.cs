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
            //propertynames from "Brand" -> #brand" in json when saving or sending data
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            //allowing matching property names regardeless of case when deserializing(recieving)
            //example Brand should be seen as brand or Brand regardless of casing
            PropertyNameCaseInsensitive = true,
            //includes public fields not just properties in serialization/deserialization
            //fields are used in records or structs
            IncludeFields = true,
        };
        public T Deserialize<T>(byte[] data)
        {
            //converts raw bytes to T using Json
            var result = JsonSerializer.Deserialize<T>(data, _options);
            if (result is null)
            {
                throw new InvalidOperationException("Kafka deserialization returned null.");
            }
            return result;
        }

        public byte[] Serialize<T>(T message)
        {
            //convert T to Json string
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _options));
        }
    }
}
