using Confluent.Kafka;
using Google.Api;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Out;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Producers
{
    internal sealed class ReocgnitionCompletedKafkaProducer : IRecognitionCompletedPublisher
    {
        private readonly ILogger _logger;
        private readonly IKafkaSerializer _serializer; 
        private readonly IOptions<KafkaProducerOptions> _options;
        private readonly IProducer<string, byte[]> _producer;

        public ReocgnitionCompletedKafkaProducer(
            ILogger<ReocgnitionCompletedKafkaProducer> logger, 
            IKafkaSerializer serializer,
            IOptions<KafkaProducerOptions> options
            )
        {
            _logger = logger;
            _serializer = serializer;
            _options = options;

            var config = new ProducerConfig
            {
                BootstrapServers = _options.Value.BootstrapServers,
                Acks = _options.Value.Acks,
                MessageSendMaxRetries = _options.Value.MessageSendMaxRetries
            };

            _producer = new ProducerBuilder<string, byte[]>(config)
                .SetKeySerializer(Serializers.Utf8)
                .SetValueSerializer(Serializers.ByteArray)
                .Build();
        }
        public Task PublishAsync(
            RecognitionResponseDto response,
            CancellationToken ct
            )
        {
            //mapping from internal dto to external Kafka event
            var completed = RecognitionCompletedMapper.ToEvent( response );
            


            throw new NotImplementedException();
        }
    }
}
