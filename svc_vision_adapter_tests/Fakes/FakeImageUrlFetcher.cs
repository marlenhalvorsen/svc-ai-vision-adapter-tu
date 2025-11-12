using svc_ai_vision_adapter.Application.Ports.Outbound;
/// <summary>
/// Fake implementation of <see cref="IImageUrlFetcher"/> that returns static presigned URLs.
/// Used to bypass external HTTP calls when testing the recognition pipeline.
/// </summary>

namespace svc_vision_adapter_tests.Fakes
{
    internal sealed class FakeImageUrlFetcher : IImageUrlFetcher
    {
        public Task<string> FetchUrlAsync(string objectKey, CancellationToken ct)
        {
            // Simulerer en presigned GET URL
            var fakeUrl = $"https://fake-storage.trackunit.test/{objectKey}?signature=fake";
            return Task.FromResult(fakeUrl);
        }
    }
}
