using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Transport;

public interface IResultAggregator
{
    MachineAggregateDto Aggregate(IReadOnlyList<ShapedResultDto> items);
}