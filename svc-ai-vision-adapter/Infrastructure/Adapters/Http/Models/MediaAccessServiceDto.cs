namespace svc_ai_vision_adapter.Infrastructure.Adapters.Http.Models
{
    public sealed record GetUrlRequest(string ObjectKey);

    public sealed record GetUrlResponse(string Url, DateTime ExpiresAt);
}
