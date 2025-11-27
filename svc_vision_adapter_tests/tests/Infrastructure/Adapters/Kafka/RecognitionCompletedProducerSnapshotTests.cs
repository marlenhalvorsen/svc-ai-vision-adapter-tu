using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Transport;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Producers;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;
using svc_ai_vision_adapter.Infrastructure.Options;



[TestClass]
public class RecognitionCompletedKafkaProducerTests
{
    private Mock<IProducer<string, byte[]>> _producerMock = null!;
    private Mock<IKafkaSerializer> _serializerMock = null!;
    private Mock<ILogger<RecognitionCompletedKafkaProducer>> _loggerMock = null!;
    private IOptions<KafkaProducerOptions> _options = null!;
    private RecognitionCompletedKafkaProducer _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _producerMock = new Mock<IProducer<string, byte[]>>();
        _serializerMock = new Mock<IKafkaSerializer>();
        _loggerMock = new Mock<ILogger<RecognitionCompletedKafkaProducer>>();

        _options = Options.Create(new KafkaProducerOptions
        {
            Topic = "tu.recognition.completed"
        });

        _sut = new RecognitionCompletedKafkaProducer(
            _loggerMock.Object,
            _serializerMock.Object,
            _options,
            _producerMock.Object
        );
    }

    [TestMethod]
    public async Task PublishAsync_SendsCorrectEventToKafka()
    {
        // ARRANGE
        var recognition = new RecognitionResponseDto(
            Ai: new AIProviderDto("vision", "v1", new List<string> { "LOGO_DETECTION" }, 5),
            Metrics: new InvocationMetricsDto(100, 1, null),
            Results: new List<ProviderResultDto>(),
            CorrelationId: "corr-123",
            ObjectKey: "obj-001",
            Compact: null,
            Aggregate: new MachineAggregateDto
            {
                Brand = "Hitachi",
                MachineType = "Excavator",
                Model = "ZX35U"
            }
        );

        var serializedBytes = Encoding.UTF8.GetBytes("{json}");

        _serializerMock.Setup(x => x.Serialize(It.IsAny<object>()))
                       .Returns(serializedBytes);

        _producerMock.Setup(x => x.ProduceAsync(
            It.IsAny<string>(),
            It.IsAny<Message<string, byte[]>>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new DeliveryResult<string, byte[]>
        {
            Status = PersistenceStatus.Persisted
        });

        Message<string, byte[]>? capturedMessage = null;

        _producerMock
            .Setup(x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, byte[]>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Message<string, byte[]>, CancellationToken>((topic, msg, ct) =>
            {
                capturedMessage = msg;
            })
            .ReturnsAsync(new DeliveryResult<string, byte[]>
            {
                Status = PersistenceStatus.Persisted
            });

        // ACT
        await _sut.PublishAsync(recognition, CancellationToken.None);

        // ASSERT — Snapshot Relevant Details
        Assert.AreEqual("obj-001", capturedMessage!.Key);
        CollectionAssert.AreEqual(serializedBytes, capturedMessage.Value);

        Assert.AreEqual("tu.recognition.completed", _options.Value.Topic);

        Assert.IsTrue(capturedMessage.Headers.TryGetLastBytes("x-correlation-id", out var corrBytes));
        Assert.AreEqual("corr-123", Encoding.UTF8.GetString(corrBytes));

        Assert.IsTrue(capturedMessage.Headers.TryGetLastBytes("x-schema", out var schemaBytes));
        Assert.AreEqual("recognition.completed.v0", Encoding.UTF8.GetString(schemaBytes));

        Assert.IsTrue(capturedMessage.Headers.TryGetLastBytes("x-producer", out var producerBytes));
        Assert.AreEqual("svc-ai-vision-adapter", Encoding.UTF8.GetString(producerBytes));
    }
}
