using svc_ai_vision_adapter.Application.Contracts;

namespace svc_ai_vision_adapter.Application.Interfaces
{
    public interface IResultShaper
    {
        ShapedResultDto Shape(ProviderResultDto raw);
    }
}
