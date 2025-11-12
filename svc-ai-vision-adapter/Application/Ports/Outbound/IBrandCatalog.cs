namespace svc_ai_vision_adapter.Application.Ports.Outbound
{
    public interface IBrandCatalog
    {
        bool IsKnownBrand(string name);
        IReadOnlyCollection<string> All { get; }
    }
}
