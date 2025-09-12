namespace svc_ai_vision_adapter.Application.Contracts
{
    /// <summary>
    /// Using ImageRef for the images to be analyzed, can be extended with other filetypes without breaking contract. 
    /// </summary>
    /// <param name="Uri"></param>
    public record ImageRefDto(string Uri);
    public record RecognitionRequestDto(
        string? sessionId,
        List<ImageRefDto> Images,
        List<string>? Features,
        string? Provider //what AI engine is going to be used, atm GoogleVisionCloud
    );
}
