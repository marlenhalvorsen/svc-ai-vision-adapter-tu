using svc_ai_vision_adapter.Application.Contracts;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Prompt
{
    internal sealed class GeminiPromptBuilder
    {
        public string BuildPrompt(MachineAggregateDto aggregate)
        {
            return $@"
                    You are an expert in identifying heavy machinery based on model numbers, brand names, or partial textual clues.

                    You may receive incomplete data. If information is incomplete, set ""partial"": true.

                    Your task is to determine:
                    - brand
                    - machine type
                    - model
                    - typical attachments
                    - estimated operating weight (kg)
                    - estimated production year range (short form, e.g. ""1994–2002"")
                    - confidence (0–1)
                    - a short ""source"" field describing what you based your reasoning on.

                    RULES:
                    - No long explanations.
                    - ""year"" must be max 12 chars (e.g. ""1994–2002"")
                    - ""source"" must be factual and short
                    - Avoid guessing
                    - If identification is unreliable: return {{""status"":""refusal"", ""reason"":""...""}}

                    Identify the following machine from extracted metadata:

                    brand: {aggregate.Brand}
                    type: {aggregate.Type}
                    model: {aggregate.Model}
                    ";
        }
    }
}
