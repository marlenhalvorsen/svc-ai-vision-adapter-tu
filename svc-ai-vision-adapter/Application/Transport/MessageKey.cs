namespace svc_ai_vision_adapter.Application.Transport
{
    /// <summary>
    /// MessageKey represent the internal values used from the kafkaEvent. 
    /// </summary>
    public record MessageKey(
        string ObjectKey,
        string? CorrelationId = null
        );
}
