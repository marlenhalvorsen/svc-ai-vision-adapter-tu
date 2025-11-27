using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Transport;

namespace svc_ai_vision_adapter.Application.Services.Shaping
{
    public interface IResultShaper
    {
        ShapedResultDto Shape(ProviderResultDto raw);
    }
}
