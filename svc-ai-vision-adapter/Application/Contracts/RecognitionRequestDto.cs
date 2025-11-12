namespace svc_ai_vision_adapter.Application.Contracts
{
    /// <summary>
    /// Using ImageRef for the images to be analyzed, can be extended with other filetypes without breaking contract. 
    /// </summary>
    /// <param name="Url"></param>
    public record ImageRefDto(string Url);
    public record RecognitionRequestDto(
        MessageKey payload
    );
    public record MessageKey(
        IReadOnlyList<string> ObjectKeys,
        string? CorrelationId = null
        );
}
