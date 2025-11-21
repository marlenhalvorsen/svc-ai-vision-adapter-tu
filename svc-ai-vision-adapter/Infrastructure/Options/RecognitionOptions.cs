// Infrastructure/Options/RecognitionOptions.cs
namespace svc_ai_vision_adapter.Infrastructure.Options
{
    public sealed class RecognitionOptions
    {
        public string DefaultProvider { get; set; } = "google";
        /// <summary>
        /// secinadary reasoning layer, enables pipeline v1
        /// </summary>
        public bool EnableReasoning { get; set; } = false;
        /// <summary>
        /// how many results the Vision providers should return 
        /// </summary>
        public int MaxResults { get; set; } = 5;
        /// <summary>
        /// featurelist for googleVision
        /// </summary>
        public List<string> Features { get; set; } = new();
        /// <summary>
        /// wheter to include raw JSON provider output
        /// </summary>
        public bool IncludeRaw { get; set; } = true; 
    }
}
