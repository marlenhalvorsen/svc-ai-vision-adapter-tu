namespace svc_ai_vision_adapter.Application.Contracts
{
    /// <summary>
    /// A generic event "envelope" for HTTP-based eventing
    /// </summary>
    public record EventEnvelope<T>(
        string Type,
        string Id,
        string? ReplyTo,
        T Data 
    );
}
