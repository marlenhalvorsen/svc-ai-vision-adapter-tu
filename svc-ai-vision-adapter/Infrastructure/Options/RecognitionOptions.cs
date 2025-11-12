// Infrastructure/Options/RecognitionOptions.cs
namespace svc_ai_vision_adapter.Infrastructure.Options
{
    public sealed class RecognitionOptions
    {
        public string DefaultProvider { get; set; } = "google";
        public int MaxResults { get; set; } = 5;
        public List<string> Features { get; set; } = new();
        public bool IncludeRaw { get; set; } = true; 
    }
}
