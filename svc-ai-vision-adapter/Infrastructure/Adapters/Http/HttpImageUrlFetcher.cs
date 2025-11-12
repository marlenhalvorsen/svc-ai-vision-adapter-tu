using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Infrastructure.Adapters.Http.Models;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.Http
{
    internal sealed class HttpImageUrlFetcher(HttpClient http) : IImageUrlFetcher
    {
        public async Task<string> FetchUrlAsync(string objectKey, CancellationToken ct)
        {
            //mapping from internal model to external api-model
            var request = new GetUrlRequest(objectKey);
            var response = await http.PostAsJsonAsync("/internal/v0/media/get-url", request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GetUrlResponse>(cancellationToken: ct);
            return result!.Url;
        }
    }
}
