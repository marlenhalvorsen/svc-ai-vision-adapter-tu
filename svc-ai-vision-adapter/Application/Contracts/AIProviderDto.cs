namespace svc_ai_vision_adapter.Application.Contracts
{

    /// <summary> 
    /// Describes what provider was used to be transparent and audit without exposing SDK-details.
    /// </summary>
    public sealed record AIProviderDto(
        string Name,
        string? ApiVersion,
        IReadOnlyList<string> Featureset,
        int? MaxResults = null,
        string? ReasoningName = null,
        string? ReasoningModel = null);
}
