using svc_ai_vision_adapter.Application.Contracts;

namespace svc_ai_vision_adapter.Application.Interfaces
{
    /// <summary>
    /// Separate port to fetch image-bytes from an URI.
    /// In this way we can test RecognitionService (mock fetch) and change protocol if needed. 
    /// </summary>
    public interface IImageFetcher
    {
        Task<(ImageRefDto Ref, byte[] Bytes)> FetchAsync(ImageRefDto img, CancellationToken ct = default);
    }
}
