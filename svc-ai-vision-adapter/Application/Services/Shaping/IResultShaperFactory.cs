namespace svc_ai_vision_adapter.Application.Services.Shaping
{
    public interface IResultShaperFactory
    {
        IResultShaper Resolve(string providerName);
    }
}
