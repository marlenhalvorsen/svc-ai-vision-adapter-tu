namespace svc_ai_vision_adapter.Application.Contracts
{
    /// <summary>
    /// Centralize string constants for event type names to avoid typos and make refactoring safer.
    /// </summary>
    public static class EventTypes
    {
        public const string VisionRequest = "ai.vision.request";
        public const string VisionResult = "ai.vision.result";
    }
}
