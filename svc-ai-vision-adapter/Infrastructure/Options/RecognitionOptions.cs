namespace svc_ai_vision_adapter.Infrastructure.Options
{
    public class RecognitionOptions
    {
        public string DefaultProvider { get; set; } = "google";
        public string? Region {get; set; }
        public int MaxResults { get; set; } = 20;
    }
}
