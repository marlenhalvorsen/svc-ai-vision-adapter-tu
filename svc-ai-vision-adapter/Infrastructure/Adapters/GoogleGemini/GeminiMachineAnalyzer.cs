using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Application.Ports.Outbound;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Models;
using svc_ai_vision_adapter.Infrastructure.Options;
using System.Text.Json;
using System.Text;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Prompt;

namespace svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini
{
    internal sealed class GeminiMachineAnalyzer : IMachineReasoningAnalyzer
    {
        private readonly HttpClient _http;
        private readonly GeminiOptions _opt;
        private readonly GeminiPromptLoader _promptLoader;

        public GeminiMachineAnalyzer(HttpClient http, IOptions<GeminiOptions> opt, GeminiPromptLoader promptLoader)
        {
            _http = http;
            _opt = opt.Value;
            _promptLoader = promptLoader;
        }

        public async Task<MachineAggregateDto> AnalyzeAsync(
            MachineAggregateDto aggregate,
            CancellationToken ct)
        {
            // 1)load prompt
            var prompt = _promptLoader.BuildPrompt(aggregate);

            // 2) load JSON Schema
            var schema = await File.ReadAllTextAsync(_opt.SchemaPath, ct);

            // 3) build user input for Gemini
            var userInput = new
            {
                model = _opt.Model,
                contents = new[]
                {
                    new {
                        role = "user",
                        parts = new [] {
                            new {
                                text = prompt +
                                       $"\n\nbrand: {aggregate.Brand}\n" +
                                       $"type: {aggregate.Type}\n" +
                                       $"model: {aggregate.Model}"
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    responseSchema = JsonSerializer.Deserialize<object>(schema)
                }
            };

            var requestContent = new StringContent(
                JsonSerializer.Serialize(userInput),
                Encoding.UTF8,
                "application/json"
            );

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_opt.Model}:generateContent?key={_opt.ApiKey}";

            // 4) call Gemini API
            var response = await _http.PostAsync(url, requestContent, ct);
            response.EnsureSuccessStatusCode();

            // 5)deserialize
            var json = await response.Content.ReadAsStringAsync(ct);

            var parsed = JsonDocument.Parse(json);
            var jsonText = parsed.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            var geminiDto = JsonSerializer.Deserialize<GeminiResponseDto>(jsonText)!;

            // 6) Map to internal aggregate
            return GeminiToAggregateMapper.Map(geminiDto);
        }
    }
}
