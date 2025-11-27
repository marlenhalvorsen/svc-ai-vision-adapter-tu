namespace svc_ai_vision_adapter.Application.Contracts
{

    public sealed record MachineAggregateDto
    {
        public string? Brand { get; init; }
        public string? MachineType { get; init; }
        public string? Model { get; init; }
        public double? Weight { get; init; }
        public string? Year { get; init; }
        public List<string>? Attachment { get; init; }

        public double Confidence { get; init; }
        public bool IsConfident { get; init; }
        public double? TypeConfidence { get; init; }
        public string? TypeSource { get; init; }

        public string Name => string.Join(", ", new[] { Brand, MachineType, Model }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim()));
    }
}
