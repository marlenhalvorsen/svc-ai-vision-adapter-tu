﻿// Infrastructure/Options/RecognitionOptions.cs
namespace svc_ai_vision_adapter.Infrastructure.Options
{
    public sealed class RecognitionOptions
    {
        public string DefaultProvider { get; set; } = "google";
        public string? Region { get; set; }
        public int MaxResults { get; set; } = 5;

        // NY: styr Vision-features via konfig
        public List<string> Features { get; set; } = new();
    }
}
