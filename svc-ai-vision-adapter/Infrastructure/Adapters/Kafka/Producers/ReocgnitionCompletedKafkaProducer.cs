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

        //asynchronos as i want to be able to log exceptions and catch exceptions.
        public async Task PublishAsync(
            RecognitionResponseDto response,
            CancellationToken ct
            )
        {            
            try
            {
                //mapping from internal dto to external Kafka event
                var completed = RecognitionCompletedMapper.ToEvent(response);

                var payload = _serializer.Serialize(completed);

                //build kafka message with key and value for _producer
                var message = new Message<string, byte[]>
                {
                    Key = completed.SessionId,
                    Value = payload
                };

                var deliveryResult = await _producer.ProduceAsync(_options.Value.Topic, message, ct);

                _logger.LogInformation(
                    "Published REcognitionCompleted event. " +
                    "Status={Status}," +
                    "Key={Key}",
                    deliveryResult.Status,
                    message.Key
                    );

            }
            catch (ProduceException<string, byte[]> ex)
            {
                _logger.LogError(ex, "Kafka produce error: {Reason}", ex.Error.Reason);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while publishing RecognitionCompleted event");
                throw;
            }
        }

        public void Dispose()
        {
            //make sure all messages are sent before disposing
            _producer.Flush(TimeSpan.FromSeconds(1));
            //as service is registered as a singleton, Dispose will automatically be called
            //when app stops
            _producer.Dispose();
        }
    }
}
