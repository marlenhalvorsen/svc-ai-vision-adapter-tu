using System.Text.Json;

namespace svc_ai_vision_adapter.Application.Transport
{

    /// <summary>
    /// Ref for image and JSON element. "RAW" is kept as an object (JSON element) so it can be debugged or used if needed. 
    /// </summary>
    public sealed record ProviderResultDto(
        ImageRefDto ImageRef,
        JsonElement Raw);

}
