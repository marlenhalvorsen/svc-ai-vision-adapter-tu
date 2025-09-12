namespace svc_ai_vision_adapter.Application.Interfaces
{
    public interface IResultShaperFactory
    {
        IResultShaper Resolve(string providerName);
    }
}
