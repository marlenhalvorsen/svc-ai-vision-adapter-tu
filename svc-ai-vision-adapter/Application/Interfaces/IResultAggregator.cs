using svc_ai_vision_adapter.Application.Contracts;

public interface IResultAggregator
{
    MachineAggregateDto Aggregate(IReadOnlyList<ShapedResultDto> items);
}