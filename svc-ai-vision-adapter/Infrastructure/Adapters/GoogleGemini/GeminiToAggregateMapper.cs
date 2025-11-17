using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Models;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini
{
    public class GeminiToAggregateMapper
    {
        public static MachineAggregateDto Map(GeminiResponseDto response)
        {
            if (response.Status == "refusal")
            {
                return new MachineAggregateDto
                {
                    Brand = null,
                    Type = null,
                    Model = null,
                    Confidence = 0,
                    IsConfident = false,
                    TypeConfidence = 0,
                    TypeSource = response.Reason
                };
            }
        }
    }
}
