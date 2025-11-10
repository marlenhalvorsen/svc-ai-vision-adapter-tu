using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Producers;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;
using System.Text.Json;

namespace svc_ai_vision_adapter_tests;

[TestClass]
public class RecognitionCompletedKafkaProducerTests
{
    [TestMethod]
    public async Task PublishAsync_UsesSerializer_AndLogsInformation()
    {
        //ARRANGE
        var mockSerializer = new Mock<IKafkaSerializer>();
        var mockLogger = new Mock<ILogger<RecognitionCompletedKafkaProducer>>();
        var mockProducer = new Mock<IProducer<string, byte[]>>();

        var fakeBytes = new byte[] {1, 2, 3, };
        var fakeResult = new DeliveryResult<string, byte[]>
        {
            Status = PersistenceStatus.Persisted,
            Message = new Message<string, byte[]>
            {
                Key = "abc123",
                Value = fakeBytes
            }
        };

        mockSerializer
            .Setup(s=>s.Serialize(It.IsAny<object>()))
            .Returns(fakeBytes);

        mockProducer
            .Setup(p=> p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, byte[]>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResult);

        var options = Options.Create(new KafkaProducerOptions
        {
            Topic = "completed"
        });

        var producer = new RecognitionCompletedKafkaProducer(
            mockLogger.Object,
            mockSerializer.Object,
            options,
            mockProducer.Object);


        var response = new RecognitionResponseDto(
                SessionId: "abc123",
                Ai: new("AI", null, null, new List<string>(), null),
                Metrics: new(120, 3, "req-123"),
                Results: new List<ProviderResultDto>
                {
                    new ProviderResultDto(
                    new ImageRefDto("img-001"),
                    JsonDocument.Parse("{}").RootElement
                    )
                },
                Compact: new List<ShapedResultDto>
                {
                    new(
                        new("img-001"),
                        new("Siemens", "CNC-Mill", "X200", 0.9, true),
                        new("Dumper", "Hitatchi", "DUMP", null, null, null),
                        Array.Empty<ObjectHitDto>()
                    )
                },
                Aggregate: new()
                {
                    Brand = "Siemens",
                    Type = "CNC-Mill",
                    Model = "X200",
                    Confidence = 0.92,
                    IsConfident = true,
                    TypeConfidence = 0.89,
                    TypeSource = "VisionModel-v3.2"
                }
            );

        //ACT
        await producer.PublishAsync(response, CancellationToken.None);

        //ASSERT
        //It.IsAny checks the method is called at least once with any type of object
        mockSerializer.Verify(s => s.Serialize(It.IsAny<object>()), Times.Once);
        mockProducer.Verify(p => p.ProduceAsync(
                    "completed",
                    It.Is<Message<string, byte[]>>(m =>
                        m.Key == "abc123" &&
                        m.Value.SequenceEqual(fakeBytes)),
                    It.IsAny<CancellationToken>()),
                    Times.Once);

        mockLogger.Verify(
            log => log.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
