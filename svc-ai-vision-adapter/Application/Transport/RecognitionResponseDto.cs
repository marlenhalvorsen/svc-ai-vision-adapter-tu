using svc_ai_vision_adapter.Application.Contracts;
using System.Text.Json;

namespace svc_ai_vision_adapter.Application.Transport
{

    /// <summary>
    /// response that is built within the recognitionService 
    /// after analyzing pictures and enriching with metadata. 
    /// Sent to RecognitionCompletedKafkaProducer. The Producer only uses
    /// AIProviderDto and MachineAggregate, other properties are included for 
    /// debugging and future development. 
    /// </summary>

    public sealed record RecognitionResponseDto(
        AIProviderDto Ai,
        InvocationMetricsDto Metrics,
        List<ProviderResultDto> Results,
        string? CorrelationId,
        string ObjectKey,
        List<ShapedResultDto>? Compact = null,
        MachineAggregateDto? Aggregate = null
        );
}
