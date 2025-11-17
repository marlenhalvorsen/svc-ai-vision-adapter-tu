using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Outbound;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini
{
    internal sealed class GeminiMachineAnalyzer : IMachineReasoningAnalyzer
    {
        public Task<MachineAggregateDto> AnalyzeAsync(MachineAggregateDto machineAggregateDto, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}
