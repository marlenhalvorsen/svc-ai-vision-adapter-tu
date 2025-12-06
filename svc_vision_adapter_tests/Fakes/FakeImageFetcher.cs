using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Application.Transport;
/// <summary>
/// Fake implementation of <see cref="IImageFetcher"/> that returns predefined byte arrays instead of fetching real images.
/// Used to simulate image download from presigned URLs during service-level testing.
/// </summary>

namespace svc_vision_adapter_tests.Fakes
{
    internal sealed class FakeImageFetcher : IImageFetcher
    {
        public Task<(ImageRefDto Ref, byte[] Bytes)> FetchAsync(ImageRefDto imageRef, CancellationToken ct)
        {
            // Returner et fake image i memory
            var bytes = new byte[] { 0x01, 0x02, 0x03 }; // dummy data
            return Task.FromResult((imageRef, bytes));
        }
    }
}
