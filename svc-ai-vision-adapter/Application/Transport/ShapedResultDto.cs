using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Models;

namespace svc_ai_vision_adapter.Application.Transport
{
    public sealed record ShapedResultDto(
        ImageRefDto ImageRef,
        MachineSummaryDto Machine,
        EvidenceDto Evidence
    );
}
