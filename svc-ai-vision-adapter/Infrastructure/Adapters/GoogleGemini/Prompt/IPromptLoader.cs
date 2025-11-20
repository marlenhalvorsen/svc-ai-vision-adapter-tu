using svc_ai_vision_adapter.Application.Contracts;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Prompt
{
    public interface IPromptLoader
    {
        string BuildPrompt(MachineAggregateDto aggregate);
    }
}
