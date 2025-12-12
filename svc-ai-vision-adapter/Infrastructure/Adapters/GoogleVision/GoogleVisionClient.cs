using Google.Cloud.Vision.V1;
using Polly;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleVision
{
    internal sealed class GoogleVisionClient : IGoogleVisionClient
    {
        private readonly ImageAnnotatorClient _client;
        private readonly IAsyncPolicy<BatchAnnotateImagesResponse> _policy;

        public GoogleVisionClient()
        {
            _client = ImageAnnotatorClient.Create();
            _policy = GoogleVisionPolicies.CreatePolicy();
        }

        public Task<BatchAnnotateImagesResponse> BatchAnnotateAsync(
            BatchAnnotateImagesRequest request,
            CancellationToken ct)
        {
            return _policy.ExecuteAsync(
                token => _client.BatchAnnotateImagesAsync(request, token),
                ct);
        }
    }
}
