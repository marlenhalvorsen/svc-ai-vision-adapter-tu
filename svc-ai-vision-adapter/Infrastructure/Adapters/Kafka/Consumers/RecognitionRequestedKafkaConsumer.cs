using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Ports.Inbound;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Consumers
{
    internal sealed class RecognitionRequestedKafkaConsumer : BackgroundService
    {
        private readonly ILogger<RecognitionRequestedKafkaConsumer> _logger;
        private readonly IConsumer<string, byte[]> _consumer;
        private readonly IKafkaSerializer _serializer;
        private readonly IServiceScopeFactory _scopeFactory;   // <-- FIX
        private readonly KafkaConsumerOptions _options;

        public RecognitionRequestedKafkaConsumer(
            ILogger<RecognitionRequestedKafkaConsumer> logger,
            IKafkaSerializer serializer,
            IServiceScopeFactory scopeFactory,                 // <-- FIX
            IOptions<KafkaConsumerOptions> options,
            IConsumer<string, byte[]> consumer)
        {
            _logger = logger;
            _serializer = serializer;
            _scopeFactory = scopeFactory;                     // <-- FIX
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
