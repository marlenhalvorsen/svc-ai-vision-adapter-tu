
namespace svc_ai_vision_adapter.Application.Contracts.Transport
{
    /// <summary>
    /// Internal application-layer DTO representing an incoming recognition request.
    /// This model is created from the external Kafka event (tu.images.uploaded)
    /// and contains the correlation ID and the list of object keys that must be
    /// processed. It is used by the RecognitionService as the canonical input
    /// format for a recognition job.
    /// </summary>
    public record RecognitionRequestDto(
        MessageKey payload
    );

}
