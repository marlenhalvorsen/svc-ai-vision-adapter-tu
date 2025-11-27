namespace svc_ai_vision_adapter.Application.Contracts.Transport
{
    /// <summary>
    /// MessageKey represent the internal values used from the kafkaEvent. 
    /// </summary>
    public record MessageKey(
        IReadOnlyList<string> ObjectKeys,
        string? CorrelationId = null
        );
}
