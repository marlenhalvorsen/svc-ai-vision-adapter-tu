using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;

namespace svc_ai_vision_adapter.Infrastructure.Http
{
    internal sealed class HttpImageFetcher(IHttpClientFactory http) : IImageFetcher
    {
        public async Task<(ImageRefDto Ref, byte[] Bytes)> FetchAsync(ImageRefDto img, CancellationToken ct = default)
        {
            var cli = http.CreateClient();

            using var resp = await cli.GetAsync(img.Uri, ct); //ensures C# uses resp.Dispose when scope has ended.
            resp.EnsureSuccessStatusCode();

            var ctHeader = resp.Content.Headers.ContentType?.MediaType ?? "";
            if (!ctHeader.Contains("image", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"not an image: {img.Uri}");

            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            if (bytes.Length == 0)
                throw new InvalidOperationException($"empty image: {img.Uri}");

            return (img, bytes);
        }
    }
}
