using Google.Cloud.Vision.V1;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision
{
    internal interface IGoogleVisionClient
    {
        Task<BatchAnnotateImagesResponse> BatchAnnotateAsync(
            BatchAnnotateImagesRequest request,
            CancellationToken ct);
    }
}
