using svc_ai_vision_adapter.Application.Contracts;

namespace svc_ai_vision_adapter.Application.Ports.Outbound
{
    /// <summary>
    /// infer machine identity based on configured vision AI output
    /// </summary>
    public interface IMachineReasoningAnalyzer
    {
        Task<MachineAggregateDto> AnalyzeAsync(
            MachineAggregateDto machineAggregateDto, 
            CancellationToken ct);
    }
}
