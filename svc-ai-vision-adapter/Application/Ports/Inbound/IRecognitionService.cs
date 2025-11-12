using svc_ai_vision_adapter.Application.Contracts;

namespace svc_ai_vision_adapter.Application.Ports.Inbound
{
    /// <summary>
    /// Public use-case boundary for image recognition.
    /// Keeps the external API surface small – consumers depend only on this,
    /// never on internal adapters or services.
    /// </summary>
    public interface IRecognitionService
    {
        Task<RecognitionResponseDto> AnalyzeAsync(MessageKey req, CancellationToken ct = default);
    }
}
