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
    public class RecognitionRequestedKafkaConsumer
    {
    }
}
