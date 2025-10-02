using System.Net;
using System.Net.Http.Headers;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Interfaces;

namespace svc_ai_vision_adapter.Infrastructure.Http
{
    internal sealed class HttpImageFetcher(IHttpClientFactory http) : IImageFetcher
    {
        // Sæt en fornuftig max størrelse (fx 10 MB) for at undgå at læse enorme filer i memory.
        private const int MaxBytes = 10 * 1024 * 1024; // 10 MB
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

        public async Task<(ImageRefDto Ref, byte[] Bytes)> FetchAsync(ImageRefDto img, CancellationToken ct = default)
        {
            if (img?.Uri == null)
                throw new ArgumentException("ImageRefDto.Uri must be set.");

            var client = http.CreateClient();
            client.Timeout = RequestTimeout;

            // Nogle hosts kræver en "rigtig" User-Agent + Accept for at tillade download.
            if (!client.DefaultRequestHeaders.UserAgent.Any())
                client.DefaultRequestHeaders.UserAgent.ParseAdd("svc-ai-vision-adapter/1.0 (+https://local.test)");

            if (!client.DefaultRequestHeaders.Accept.Any())
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));

            // Simpel retry på transient fejl
            var attempt = 0;
            while (true)
            {
                attempt++;
                using var resp = await client.GetAsync(img.Uri, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

                // Special-case: 403 → ofte UA/Accept/CDN-regel. Giv en tydelig fejl.
                if (resp.StatusCode == HttpStatusCode.Forbidden)
                    throw new InvalidOperationException($"403 Forbidden fetching image: {img.Uri}. Some hosts require a non-empty User-Agent or block non-browser requests.");

                // Ved andre ikke-2xx: kast med detaljer
                if (!resp.IsSuccessStatusCode)
                {
                    // Retry på 408/429/5xx op til 3 forsøg
                    if (IsTransient(resp.StatusCode) && attempt < 3)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct);
                        continue;
                    }

                    var status = (int)resp.StatusCode;
                    throw new HttpRequestException($"HTTP {status} when fetching image: {img.Uri}");
                }

                // Tjek content-type (tillad image/* og application/octet-stream)
                var mediaType = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
                var isImageLike =
                    (mediaType != null && mediaType.StartsWith("image/")) ||
                    string.Equals(mediaType, "application/octet-stream", StringComparison.Ordinal);

                if (!isImageLike)
                    throw new InvalidOperationException($"Not an image (Content-Type={mediaType ?? "null"}): {img.Uri}");

                // Tjek Content-Length hvis den findes
                if (resp.Content.Headers.ContentLength is long len && len > MaxBytes)
                    throw new InvalidOperationException($"Image too large ({len} bytes > {MaxBytes}). Uri: {img.Uri}");

                // Stream til memory med grænse
                await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                var bytes = await ReadAllBytesWithLimitAsync(stream, MaxBytes, ct).ConfigureAwait(false);

                if (bytes.Length == 0)
                    throw new InvalidOperationException($"Empty image: {img.Uri}");

                // (Valgfrit) hurtig magic-number check, hvis Content-Type var tvivlsom.
                // if (mediaType == "application/octet-stream" && !LooksLikeCommonImage(bytes))
                //     throw new InvalidOperationException($"Octet-stream did not look like an image by signature: {img.Uri}");

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

        // (Valgfrit) minimal file-signature check for JPEG/PNG/GIF/WEBP
        // private static bool LooksLikeCommonImage(byte[] bytes)
        // {
        //     if (bytes.Length < 12) return false;
        //     // JPEG: FF D8
        //     if (bytes[0] == 0xFF && bytes[1] == 0xD8) return true;
        //     // PNG: 89 50 4E 47 0D 0A 1A 0A
        //     if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;
        //     // GIF: 47 49 46
        //     if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return true;
        //     // WEBP: "RIFF"...."WEBP"
        //     if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
        //         bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return true;
        //     return false;
        // }
    }
}
