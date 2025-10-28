using System.Threading;
using System.Threading.Tasks;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.In;
using svc_ai_vision_adapter.Application.Ports.Out;

// This handler is the entry point for asynchronous "RecognitionRequested" work.
// It is called by Infrastructure (Kafka consumer) whenever a new request comes in.
// Responsibilities:
// 1. Call the RecognitionService to perform the actual analysis work
// 2. Take the RecognitionResponseDto result
// 3. Publish an outgoing "RecognitionCompleted" event via IRecognitionCompletedPublisher
namespace svc_ai_vision_adapter.Application.MessageHandling
{
    internal sealed class RecognitionRequestedHandler : IRecognitionRequestedHandler
    {
        private readonly IRecognitionService _recognitionService;

        private readonly IRecognitionCompletedPublisher _publisher;

        public RecognitionRequestedHandler(
            IRecognitionService recognitionService, 
            IRecognitionCompletedPublisher publisher)
        {
            _recognitionService = recognitionService;
            _publisher = publisher;
        }

        public async Task HandleAsync(RecognitionRequestDto request, CancellationToken ct)
        {
            RecognitionResponseDto response = await _recognitionService.AnalyzeAsync(request, ct);

            await _publisher.PublishAsync(response, ct); 
        }
    }
}
