using svc_ai_vision_adapter.Application.Contracts;

namespace svc_ai_vision_adapter.Application.Ports.Out
{
    public interface IImageUrlFetcher
    {
        /// <summary>
        /// Outbound port that fetches presignedUrl to pass to IImageFetcher
        /// </summary>
        /// <param name="objectKey'"></param>
        /// object key is posted in an event from topic consumer listens to
        /// <returns></returns>
        Task<string> FetchUrlAsync(string objectKey, CancellationToken ct = default!);
    }
}
