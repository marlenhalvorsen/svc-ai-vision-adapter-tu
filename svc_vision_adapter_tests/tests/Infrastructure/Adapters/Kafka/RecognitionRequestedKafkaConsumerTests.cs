using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.In;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Consumers;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Models;
using svc_ai_vision_adapter.Infrastructure.Adapters.Kafka.Serialization;

namespace svc_vision_adapter_tests.Infrastructure.Kafka.Consumers
{
    [TestClass]
    public class RecognitionRequestedKafkaConsumerTests
    {
        private Mock<IKafkaSerializer> _serializerMock;
        private Mock<IRecognitionRequestedHandler> _handlerMock;
        private Mock<ILogger<RecognitionRequestedKafkaConsumer>> _loggerMock;
        private Mock<IConsumer<string, byte[]>> _consumerMock;
        private IOptions<KafkaConsumerOptions> _options;
        private RecognitionRequestedKafkaConsumer _sut;

        [TestInitialize]
        public void Setup()
        {
            _serializerMock = new Mock<IKafkaSerializer>();
            _handlerMock = new Mock<IRecognitionRequestedHandler>();
            _loggerMock = new Mock<ILogger<RecognitionRequestedKafkaConsumer>>();
            _consumerMock = new Mock<IConsumer<string, byte[]>>();

            var fakeBytes = new byte[] { 1, 2, 3 };

            _consumerMock
                .Setup(c => c.Consume(It.IsAny<CancellationToken>()))
                .Returns(new ConsumeResult<string, byte[]>
                {
                    Message = new Message<string, byte[]>
                    {
                        Key = "object-key",
                        Value = new byte[] { 1, 2, 3 },
                        Headers = new Headers()
                    },
                    TopicPartitionOffset = new TopicPartitionOffset("tu.images.uploaded", 0, 42)
                });

            _options = Options.Create(new KafkaConsumerOptions
            {
                BootstrapServers = "fake-server",
                GroupId = "test-group",
                Topic = "tu.images.uploaded",
                EnableAutoCommit = false
            });

            _sut = new RecognitionRequestedKafkaConsumer(
                _loggerMock.Object,
                _serializerMock.Object,
                _handlerMock.Object,
                _options,
                _consumerMock.Object);
        }

        [TestMethod]
        public async Task ProcessKafkaMessage_Should_Call_Handler_With_Mapped_MessageKey()
        {
            // Arrange
            var externalEvent = new ImageUploadedEvent
            {
                ObjectKey = "images/2025/11/02/img.jpg",
                CorrelationId = "corr-001"
            };
            var fakeBytes = new byte[] { 1, 2, 3 };

            _serializerMock
                .Setup(s => s.Deserialize<ImageUploadedEvent>(It.IsAny<byte[]>()))
                .Returns(externalEvent);

            // Act
            await _sut.ProcessKafkaMessage(CancellationToken.None);

            // Assert
            _handlerMock.Verify(h =>
                h.HandleAsync(It.Is<MessageKey>(m =>
                    m.ObjectKeys.Any(k => k.Contains("img.jpg")) &&   
                    m.CorrelationId == "corr-001"),
                It.IsAny<CancellationToken>()),
                Times.Once);


            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task ProcessKafkaMessage_Should_LogError_When_Deserialization_Fails()
        {
            // Arrange
            _serializerMock
                .Setup(s => s.Deserialize<ImageUploadedEvent>(It.IsAny<byte[]>()))
                .Throws(new System.Text.Json.JsonException("Invalid JSON"));

            // Act
            await _sut.ProcessKafkaMessage(CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<System.Exception>(),
                    (Func<It.IsAnyType, System.Exception, string>)It.IsAny<object>()),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task ProcessKafkaMessage_Should_LogError_When_Handler_Fails()
        {
            // Arrange
            var externalEvent = new ImageUploadedEvent
            {
                ObjectKey = "images/2025/11/02/img.jpg",
                CorrelationId = "corr-999"
            };

            _serializerMock
                .Setup(s => s.Deserialize<ImageUploadedEvent>(It.IsAny<byte[]>()))
                .Returns(externalEvent);

            _handlerMock
                .Setup(h => h.HandleAsync(It.IsAny<MessageKey>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Handler failed"));

            // Act
            await _sut.ProcessKafkaMessage(CancellationToken.None);

            // Assert
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<System.Exception>(),
                    (Func<It.IsAnyType, System.Exception, string>)It.IsAny<object>()),
                Times.AtLeastOnce);

        }
    }
}
