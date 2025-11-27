using Confluent.Kafka;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;
using svc_ai_vision_adapter.Infrastructure.Options;
using System.Text;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Producers
{
    internal sealed class RecognitionCompletedKafkaProducer : IRecognitionCompletedPublisher
    {
        private readonly ILogger _logger;
        private readonly IKafkaSerializer _serializer; 
        private readonly IOptions<KafkaProducerOptions> _options;
        private readonly IProducer<string, byte[]> _producer;

        public RecognitionCompletedKafkaProducer(
            ILogger<RecognitionCompletedKafkaProducer> logger, 
            IKafkaSerializer serializer,
            IOptions<KafkaProducerOptions> options,
            IProducer<string, byte[]> producer
            )
        {
            _logger = logger;
            _serializer = serializer;
            _options = options;
            _producer = producer;
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

                var headers = new Headers
                {   
                    { "x-correlation-id", Encoding.UTF8.GetBytes(response.CorrelationId ?? Guid.NewGuid().ToString()) },
                    { "x-schema", Encoding.UTF8.GetBytes("recognition.completed.v0") },
                    { "x-producer", Encoding.UTF8.GetBytes("svc-ai-vision-adapter") }
                };

                //build kafka message with key and value for _producer
                var message = new Message<string, byte[]>
                {
                    Key = response.ObjectKey,
                    Value = payload,
                    Headers = headers
                };

                var deliveryResult = await _producer.ProduceAsync(_options.Value.Topic, message, ct);

                _logger.LogInformation(
                    "Published RecognitionCompleted event. " +
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
