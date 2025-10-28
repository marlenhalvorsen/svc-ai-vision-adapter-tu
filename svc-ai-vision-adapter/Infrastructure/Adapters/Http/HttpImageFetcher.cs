using System.Net;
using System.Net.Http.Headers;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Out;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.Http
{
    internal sealed class HttpImageFetcher(IHttpClientFactory http) : IImageFetcher
    {
        // Max size to avoid enormous files in memory
        private const int MaxBytes = 10 * 1024 * 1024; // 10 MB
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

        public async Task<(ImageRefDto Ref, byte[] Bytes)> FetchAsync(ImageRefDto img, CancellationToken ct = default)
        {
            if (img?.Uri == null)
                throw new ArgumentException("ImageRefDto.Uri must be set.");

            var client = http.CreateClient();
            client.Timeout = RequestTimeout;

            // some req user agent and agent for download
            if (!client.DefaultRequestHeaders.UserAgent.Any())
                client.DefaultRequestHeaders.UserAgent.ParseAdd("svc-ai-vision-adapter/1.0 (+https://local.test)");

            if (!client.DefaultRequestHeaders.Accept.Any())
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

            // retry for transient error
            var attempt = 0;
            while (true)
            {
                attempt++;
                using var resp = await client.GetAsync(img.Uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

                // Special-case: 403 → UA/Accept/CDN-rule. Make it clear what mistake it is.
                if (resp.StatusCode == HttpStatusCode.Forbidden)
                    throw new InvalidOperationException($"403 Forbidden fetching image: {img.Uri}. Some hosts require a non-empty User-Agent or block non-browser requests.");

                if (!resp.IsSuccessStatusCode)
                {
                    // Retry 408/429/5xx up to 3 tries
                    if (IsTransient(resp.StatusCode) && attempt < 3)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct);
                        continue;
                    }

                    var status = (int)resp.StatusCode;
                    throw new HttpRequestException($"HTTP {status} when fetching image: {img.Uri}");
                }

                // check content type
                var mediaType = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                var isImageLike =
                    mediaType != null && mediaType.StartsWith("image/") ||
                    string.Equals(mediaType, "application/octet-stream", StringComparison.Ordinal);

                if (!isImageLike)
                    throw new InvalidOperationException($"Not an image (Content-Type={mediaType ?? "null"}): {img.Uri}");

                // check length
                if (resp.Content.Headers.ContentLength is long len && len > MaxBytes)
                    throw new InvalidOperationException($"Image too large ({len} bytes > {MaxBytes}). Uri: {img.Uri}");

                // stream to in memory (with max)
                await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                var bytes = await ReadAllBytesWithLimitAsync(stream, MaxBytes, ct).ConfigureAwait(false);

                if (bytes.Length == 0)
                    throw new InvalidOperationException($"Empty image: {img.Uri}");

                return (img, bytes);
            }
        }

        private static bool IsTransient(HttpStatusCode code) =>
            code == HttpStatusCode.RequestTimeout // 408
            || code == (HttpStatusCode)429        // Too Many Requests
            || (int)code >= 500;                  // 5xx

        private static async Task<byte[]> ReadAllBytesWithLimitAsync(Stream src, int maxBytes, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            int total = 0;

            while ((read = await src.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > maxBytes)
                    throw new InvalidOperationException($"Image exceeded maximum allowed size of {maxBytes} bytes.");
                ms.Write(buffer, 0, read);
            }

            return ms.ToArray();
        }

    }
}
