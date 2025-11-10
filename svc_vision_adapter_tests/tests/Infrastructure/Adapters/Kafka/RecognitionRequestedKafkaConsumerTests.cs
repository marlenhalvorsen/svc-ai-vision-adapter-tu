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
        private IOptions<KafkaConsumerOptions> _options;
        private RecognitionRequestedKafkaConsumer _sut; // system under test
        private Mock<IConsumer<string, byte[]>> _consumerMock;

        [TestInitialize]
        public void Setup()
        {
            _handlerMock = new Mock<IRecognitionRequestedHandler>();
            _loggerMock = new Mock<ILogger<RecognitionRequestedKafkaConsumer>>();
            _consumerMock = new Mock<IConsumer<string, byte[]>>();
            _serializerMock = new Mock<IKafkaSerializer>();

            var fakeBytes = new byte[] { 1, 2, 3 };
            _consumerMock
                .Setup(c => c.Consume(It.IsAny<CancellationToken>()))
                .Returns(new ConsumeResult<string, byte[]>
                {
                    Message = new Message<string, byte[]> { Key = "session-123", Value = fakeBytes }
                });
            _serializerMock
                .Setup(s => s.Deserialize<RecognitionRequestedEvent>(fakeBytes))
                .Returns(new RecognitionRequestedEvent
                {
                    RequestID = "session-123",
                    Provider = "GVC",
                    ImageUrls = new List<ImageRefDto> { new("http://img.jpg") }
                });
            _options = Options.Create(new KafkaConsumerOptions
            {
                BootstrapServers = "fake-server",
                GroupId = "test-group",
                Topic = "recognition-requested",
                EnableAutoCommit = false
            });

            //own consumer during test
            _sut = new RecognitionRequestedKafkaConsumer(
                _loggerMock.Object,
                _serializerMock.Object,
                _handlerMock.Object,
                _options, _consumerMock.Object
            );
        }

        [TestMethod]
        public async Task ProcessKafkaMessage_Should_Call_Handler_With_Deserialized_Event()
        {
            // Arrange
            var externalEvent = new RecognitionRequestedEvent
            {
                RequestID = "session-123",
                Provider = "GVC",
                ImageUrls = new List<ImageRefDto>
                {
                    new ImageRefDto ( "http://img.jpg") 
                }
            }; 
            var fakeBytes = new byte[] { 1, 2, 3 };

            _serializerMock
                .Setup(s => s.Deserialize<RecognitionRequestedEvent>(It.IsAny<byte[]>()))
                .Returns(externalEvent);

            // Act
            await _sut.ProcessKafkaMessage(CancellationToken.None);

            // Assert
            _handlerMock.Verify(h =>
                h.HandleAsync(It.Is<RecognitionRequestDto>(
                    dto => dto.SessionId == "session-123"),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<System.Exception>(),
                    (Func<It.IsAnyType, System.Exception, string>)It.IsAny<object>()),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task ProcessKafkaMessage_Should_LogError_When_Deserialization_Fails()
        {
            // Arrange
            _serializerMock
                .Setup(s => s.Deserialize<RecognitionRequestedEvent>(It.IsAny<byte[]>()))
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
            var externalEvent = new RecognitionRequestedEvent
            {
                RequestID = "abc",
                Provider = "GVC",
                ImageUrls = new List<ImageRefDto> { new ImageRefDto ( "img.jpg") }
            };
            _serializerMock
                .Setup(s => s.Deserialize<RecognitionRequestedEvent>(It.IsAny<byte[]>()))
                .Returns(externalEvent);

            _handlerMock
                .Setup(h => h.HandleAsync(It.IsAny<svc_ai_vision_adapter.Application.Contracts.RecognitionRequestDto>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Handler failed"));

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
