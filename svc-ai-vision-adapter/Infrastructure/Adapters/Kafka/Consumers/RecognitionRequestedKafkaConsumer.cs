using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting; //backgroundservice
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Ports.In;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;
// This class is the Kafka consumer adapter.
// It runs in the background (via BackgroundService) and listens to a Kafka topic
// for "RecognitionRequested" events.
// For each message:
//   1. Read the raw Kafka message (byte[] payload)
//   2. Deserialize it into RecognitionRequestedEvent (external contract / wire model)
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
        //from Confluent.Kafka
        private readonly IConsumer<string, byte[]> _consumer;
        private readonly IKafkaSerializer _serializer;
        private readonly IRecognitionRequestedHandler _handler;
        private readonly KafkaConsumerOptions _options;

        public RecognitionRequestedKafkaConsumer(
            ILogger<RecognitionRequestedKafkaConsumer> logger, 
            IKafkaSerializer serializer, 
            IRecognitionRequestedHandler handler, 
            IOptions<KafkaConsumerOptions> options)
        {
            _logger = logger;
            _serializer = serializer;
            _handler = handler;
            _options = options.Value;

            var config = new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                GroupId = _options.GroupId,
                EnableAutoCommit = _options.EnableAutoCommit,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            //deserialize the key as a string and the value as byte[]
            _consumer = new ConsumerBuilder<string, byte[]>(config)
                .SetKeyDeserializer(Deserializers.Utf8) //sets key to string (from bytes)
                .SetValueDeserializer(Deserializers.ByteArray)//sets value to bytes[]
                .Build();
        }
        //Main loop for BackgroundService.
        //This runs until the host shuts down or until cancellation is requested
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
            //in executeAsync to minimize error logs and ensure closing of the consumer
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
                //takes the raw bytes and turns into our external event model
                RecognitionRequestedEvent externalEvent = 
                    _serializer.Deserialize<RecognitionRequestedEvent>(consumeResult.Message.Value);
                //maps the external event to internal Dto(RecognitionRequestDto)
                var internalRequestDto = RecognitionRequestedMapper.ToDto(externalEvent);

                //calls upon applicationLayer
                await _handler.HandleAsync(internalRequestDto, stoppingToken);

                _logger.LogInformation("Processed RecognitionRequested event: RequestId={ReuestId}: " +
                    "Offset={Offset}", 
                    externalEvent.RequestID, 
                    //tuple with Kafka topic name and partition
                    consumeResult.TopicPartitionOffset);
            }
            
            catch (ConsumeException ex) 
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing Kafka event");
            }
        }

        public override void Dispose()
        {
            //commit final offsets and leave the consumer group nicely.
            _consumer.Close(); 
            //frees unmanaged resources used by the consumer.
            _consumer.Dispose();

            base.Dispose();
        }
    }
}
