using Confluent.Kafka;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Ports.Inbound;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;

using svc_ai_vision_adapter.Infrastructure.Options;
// This class is the Kafka consumer adapter.
// It runs in the background (via BackgroundService) and listens to a Kafka topic
// for "RecognitionRequested" events.
// For each message:
//   1. Read the raw Kafka message (objectKey)
//   2. Deserialize it into ImageUploadedEvent (external contract / wire model)
//   3. Map it into RecognitionRequestDto (our internal request DTO)
//   4. Call the application layer via IRecognitionRequestedHandler
// IMPORTANT ARCHITECTURE POINTS:
// - This class lives in Infrastructure because it talks directly to Kafka.
// - It depends on Application only through the port IRecognitionRequestedHandler.
//   Application does NOT depend on this class. That keeps the direction of dependencies clean.
namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Consumers
{
    internal sealed class RecognitionRequestedKafkaConsumer : BackgroundService
    {
        private readonly ILogger<RecognitionRequestedKafkaConsumer> _logger;
        private readonly IConsumer<string, byte[]> _consumer;
        private readonly IKafkaSerializer _serializer;
        private readonly IServiceScopeFactory _scopeFactory;   
        private readonly KafkaConsumerOptions _options;

        public RecognitionRequestedKafkaConsumer(
            ILogger<RecognitionRequestedKafkaConsumer> logger,
            IKafkaSerializer serializer,
            IServiceScopeFactory scopeFactory,                
            IOptions<KafkaConsumerOptions> options,
            IConsumer<string, byte[]> consumer)
        {
            _logger = logger;
            _serializer = serializer;
            _scopeFactory = scopeFactory;                     
            _options = options.Value;
            _consumer = consumer;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _consumer.Subscribe(_options.Topic);

            _logger.LogInformation(
                "Kafka consumer started. Topic={Topic} GroupId={GroupId}",
                _options.Topic,
                _options.GroupId);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await ProcessKafkaMessage(ct);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka consumer is stopping");
            }
            finally
            {
                _consumer.Close();
            }
        }

        public async Task ProcessKafkaMessage(CancellationToken stoppingToken)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                ImageUploadedEvent externalEvent =
                 _serializer.Deserialize<ImageUploadedEvent>(consumeResult.Message.Value);

                //extract correlationId from kafka header
                var correlationIdHeader = consumeResult.Message.Headers
                    .FirstOrDefault(h => h.Key == "x-correlation-id")
                    ?.GetValueBytes();

                string? correlationId =
                    correlationIdHeader is not null
                        ? System.Text.Encoding.UTF8.GetString(correlationIdHeader)
                        : null;

                var internalDto = RecognitionRequestedMapper.ToDto(externalEvent, correlationId);

                //in hosted services (BackgroundService), create scopes inside the work loop,
                //not in the host builder.
                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IRecognitionRequestedHandler>();
                await handler.HandleAsync(internalDto, stoppingToken);

                _logger.LogInformation(
                    "Processed ImageUploaded event: ObjectKey={ObjectKey}, CorrelationId={CorrelationId}, Offset={Offset}",
                    externalEvent.ObjectKey,
                    correlationId,
                    consumeResult.TopicPartitionOffset);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Kafka event");
            }
        }

        public override void Dispose()
        {
            _consumer.Close();
            _consumer.Dispose();
            base.Dispose();
        }
    }
}
