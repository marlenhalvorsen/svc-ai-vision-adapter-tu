namespace svc_ai_vision_adapter.Application.Interfaces
{
    public interface IBrandCatalog
    {
        bool IsKnownBrand(string name);
        IReadOnlyCollection<string> All { get; }
    }
}
