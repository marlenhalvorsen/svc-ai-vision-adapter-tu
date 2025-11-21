using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Models;


namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini
{
    /// <summary>
    /// static as it is not to be instantiatied - only to be used as a mapper
    /// </summary>
    internal static class GeminiToAggregateMapper
    {
        internal static MachineAggregateDto Map(GeminiResponseDto response)
        {
            if (response.Status == "refusal")
            {
                return new MachineAggregateDto
                {
                    Brand = null,
                    MachineType = null,
                    Model = null,
                    Confidence = 0,
                    IsConfident = false,
                    TypeSource = response.Reason
                };
            }
            return new MachineAggregateDto
            {
                Brand = response.Brand,
                MachineType = response.MachineType,
                Model = response.Model,
                Weight = response.Weight,
                Year = response.Year,
                Attachment = response.Attachment,
                Confidence = response.Confidence ?? 0,
                IsConfident = (response.Confidence ?? 0) > 0.75,
                TypeSource = response.Source
            };
        }
    }
}
